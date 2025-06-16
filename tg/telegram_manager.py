import asyncio
import random
import re
import os
import pathlib
from datetime import datetime
from loguru import logger
from collections import deque
import openpyxl
import argparse
import json
import pandas as pd

# Определяем базовую директорию относительно расположения скрипта
BASE_DIR = pathlib.Path(__file__).parent
SIMILAR_DIR = BASE_DIR / "similar"
SIMILAR_DIR.mkdir(exist_ok=True)

# Константы для путей
SIMILAR_DIR = os.getenv('SIMILAR_DIR', str(BASE_DIR / "similar"))  # Путь к директории similar, по умолчанию 'similar'

from telethon import TelegramClient
from telethon.errors import (
    UserAlreadyParticipantError,
    InviteRequestSentError,
    InviteHashExpiredError,
    AuthKeyDuplicatedError,
    PhoneNumberInvalidError
)
from telethon.tl.functions.messages import ImportChatInviteRequest
from telethon.tl.functions.channels import JoinChannelRequest, GetChannelRecommendationsRequest

# Импортируем библиотеки для конвертации сессий
try:
    from opentele.td import TDesktop
    from opentele.tl import TelegramClient as OpenTeleClient
    from opentele.api import UseCurrentSession
    OPENTELE_AVAILABLE = True
except ImportError:
    logger.warning("Библиотека opentele не установлена. Функция конвертации сессий недоступна.")
    OPENTELE_AVAILABLE = False

from config import TELEGRAM_CONFIG
from channel_parser import TelegramChannel
from session_manager import (
    handle_flood_wait, 
    check_authorization, 
    handle_un_auth_error, 
    calculate_delay
)

from utils import (
    append_data_to_file,
    readFileToStrips,

)

async def get_similar_channels(client: TelegramClient, channel_identifier: str):
    """
    Получает похожие каналы от Telegram API.
    
    Args:
        client: Клиент Telegram
        channel_identifier: Идентификатор канала
        
    Returns:
        list: Список похожих каналов
    """
    try:
        # Преобразуем входную строку в InputChannel (Telethon сам выполнит конвертацию)
        input_channel = await client.get_entity(channel_identifier)
        
        # Вызываем официальный метод получения рекомендаций
        try:
            result = await client(GetChannelRecommendationsRequest(channel=input_channel))
            
            # Если поле chats присутствует, возвращаем его, иначе – пустой список
            similar_channels = result.chats if hasattr(result, "chats") else []
            
            # Улучшенное логирование с отображением информации о каналах
            channel_info = []
            for channel in similar_channels:
                title = getattr(channel, 'title', 'Без названия')
                username = getattr(channel, 'username', None)
                channel_id = getattr(channel, 'id', 'Неизвестный ID')
                link = f"@{username}" if username else f"ID: {channel_id}"
                channel_info.append(f"{title} ({link})")
            
            # logger.info(
            #     f"🔍 Результат GetChannelRecommendations для {channel_identifier}:\n" + 
            #     "\n".join([f"- {info}" for info in channel_info]) if channel_info else "Похожие каналы не найдены"
            # )
            return similar_channels
        except ConnectionError as e:
            logger.error(f"Ошибка сетевого соединения при получении рекомендаций для {channel_identifier}: {e}")
            return []
        except asyncio.TimeoutError:
            logger.error(f"Таймаут при получении рекомендаций для {channel_identifier}")
            return []
        except Exception as e:
            logger.error(f"Ошибка при выполнении GetChannelRecommendationsRequest для {channel_identifier}: {e}")
            return []
            
    except ConnectionError as e:
        logger.error(f"Ошибка сетевого соединения при получении entity для {channel_identifier}: {e}")
        return []
    except asyncio.TimeoutError:
        logger.error(f"Таймаут при получении entity для {channel_identifier}")
        return []
    except Exception as e:
        logger.error(f"Ошибка получения похожих каналов для {channel_identifier}: {e}")
        return []


def unique_key(entity) -> str:
    """
    Формирует уникальный ключ для канала.
    Если у канала есть username, возвращает '@username',
    иначе — строковое представление его ID.
    """
    if hasattr(entity, 'username') and entity.username:
        return f"@{entity.username}"
    else:
        return str(entity.id)


async def collect_similar_channels(client: TelegramClient, donor_identifier: str, max_depth: int = 2):
    """
    Собирает данные по каналам, начиная от донорского (глубина 0),
    затем похожих каналов (глубина 1) и похожих для похожих (глубина 2).
    
    Для каналов уровня 2, если канал ранее не встречался на уровне 1,
    в столбце "Донор второго уровня" записывается от какого канала он появился.
    Подсчитывается, сколько раз канал встретился (intersections).

    Возвращает словарь, где ключ — уникальный идентификатор канала (username или ID),
    а значение — словарь с данными:
      {
         'title': <название>,
         'link': <ссылка или ID>,
         'subs': <participants_count>,
         'depth': <глубина>,
         'intersections': <число>,
         'second_level_donor': <если depth == 2, имя родительского канала, иначе пустая строка>
      }
    """
    channels_map = {}
    visited = set()  # для избежания повторной обработки
    # Очередь: (идентификатор для запроса, глубина, donor второго уровня)
    queue = deque()
    # Начинаем с донорского канала (глубина 0, donor второго уровня пустой)
    queue.append((donor_identifier, 0, ""))
    
    # Счетчик запросов для добавления дополнительных задержек
    request_counter = 0
    # Базовая задержка между запросами (в секундах)
    base_delay = 60  # 1 минута между запросами к одному аккаунту

    while queue:
        current_id, depth, donor2 = queue.popleft()

        # Если канал уже в нашем словаре, увеличим счетчик пересечений и пропустим дальнейшую обработку
        if current_id in channels_map:
            channels_map[current_id]['intersections'] += 1
            continue

        # Добавляем задержку перед каждым запросом для защиты от блокировки
        request_counter += 1
        if request_counter > 1:  # Пропускаем задержку для первого запроса
            # Добавляем вариацию для избежания шаблонности запросов
            delay = base_delay * random.uniform(0.8, 1.2)
            logger.info(f"Делаю паузу {delay:.1f} секунд перед запросом #{request_counter} для {current_id}")
            await asyncio.sleep(delay)

        # Получаем данные из Telethon: здесь вызываем get_entity чтобы получить данные из current_id
        try:
            entity = await client.get_entity(current_id)
            logger.info(f"Получены данные для канала: {current_id}")
            
            # Дополнительная задержка после запроса entity
            await asyncio.sleep(20 * random.uniform(0.8, 1.2))
        except Exception as e:
            logger.error(f"Ошибка получения entity для {current_id}: {e}")
            continue

        key = unique_key(entity)
        # Если текущий ключ отличается от переданного current_id (например, донор задан как "@channel"), используем его
        current_key = key

        # Извлекаем необходимые данные
        title = entity.title if hasattr(entity, 'title') else "Без названия"
        if hasattr(entity, 'username') and entity.username:
            link = f"https://t.me/{entity.username}"
        else:
            link = f"ID: {entity.id}"
        subs = getattr(entity, "participants_count", 0)

        channels_map[current_key] = {
            'title': title,
            'link': link,
            'subs': subs,
            'depth': depth,
            'intersections': 1,
            # Если глубина равна 2 и канал не был получен напрямую от донорского (то есть donor2 не пустой), запишем его,
            # иначе оставляем пустой строкой
            'second_level_donor': donor2 if depth == 2 else "",
            'donor_position': ""  # По умолчанию пусто
        }
        visited.add(current_key)

        # Если еще не достигли максимальной глубины, получаем похожие каналы
        if depth < max_depth:
            # Добавляем увеличенную задержку перед запросом похожих каналов
            # logger.info(f"Делаю паузу перед запросом похожих каналов для {current_key}")
            # await asyncio.sleep(40 * random.uniform(1, 1.1))  # 3 минуты ± 10%
            
            similar_entities = await get_similar_channels(client, current_key)
            
            # Добавляем ещё одну задержку после получения похожих каналов
            await asyncio.sleep(60 * random.uniform(0.8, 1.2))
            
            next_depth = depth + 1
            for sim in similar_entities:
                sim_key = unique_key(sim)
                # Если канал уже встречался на более раннем уровне (например, в donor-подобных), то оставляем поле "донор второго уровня" пустым
                next_donor = current_key if next_depth == 2 and sim_key not in channels_map else ""
                queue.append((sim_key, next_depth, next_donor))
    return channels_map


def parse_subscribers_count(subs):
    """
    Преобразует значение количества подписчиков в целое число.
    Обрабатывает различные форматы: числа, строки с разделителями, None.
    """
    if subs is None:
        return 0
    if isinstance(subs, int):
        return subs
    if isinstance(subs, str):
        # Удаляем все нецифровые символы
        subs = ''.join(filter(str.isdigit, subs))
        return int(subs) if subs else 0
    return 0

def build_excel_table(channels_data: dict, output_file: str = "similar_channels.xlsx"):
    """
    Строит Excel-таблицу с данными из channels_data.
    Данные содержат следующие поля:
      - Название
      - Ссылка
      - Количество подписчиков
      - Глубина
      - Пересечения
      - Донор второго уровня
      - Положение донорского канала в списке похожих
    """
    try:
        logger.info(f"Начинаю создание Excel-таблицы. Путь к файлу: {output_file}")
        
        # Создаем список для сортировки
        sorted_data = []
        for key, data in channels_data.items():
            sorted_data.append((key, data))
        
        # Сортируем список по убыванию числа пересечений
        sorted_data.sort(key=lambda x: x[1]['intersections'], reverse=True)

        # Создаем список словарей с данными
        result = []
        for key, data in sorted_data:
            result.append({
                'title': data['title'],
                'link': data['link'],
                'subs': parse_subscribers_count(data['subs']),
                'depth': data['depth'],
                'intersections': data['intersections'],
                'second_level_donor': data['second_level_donor'],
                'donor_position': data.get('donor_position', '')
            })

        logger.info(f"Данные подготовлены. Количество записей: {len(result)}")

        # Создаем DataFrame и сохраняем в Excel
        df = pd.DataFrame(result)
        logger.info("DataFrame создан успешно")
        
        # Создаем директорию для файла, если она не существует
        os.makedirs(os.path.dirname(output_file), exist_ok=True)
        logger.info(f"Директория для файла создана/проверена: {os.path.dirname(output_file)}")
        
        # Сохраняем в Excel
        df.to_excel(output_file, index=False)
        logger.info(f"Данные успешно сохранены в файл: {output_file}")
        
        # Проверяем, что файл действительно создался
        if os.path.exists(output_file):
            logger.info(f"Файл успешно создан: {output_file}")
            logger.info(f"Размер файла: {os.path.getsize(output_file)} байт")
        else:
            logger.error(f"Файл не был создан: {output_file}")
        
        # Выводим JSON в стандартный вывод
        print(json.dumps(result, ensure_ascii=False))
        logger.info(f"Данные успешно сформированы в JSON!")
        
    except Exception as e:
        logger.error(f"Ошибка при создании Excel-таблицы: {str(e)}")
        logger.error(f"Тип ошибки: {type(e)}")
        import traceback
        logger.error(f"Traceback: {traceback.format_exc()}")
        raise


async def run_similar_channels_analysis(session_path: str, donor_channel: str, output_file: str = "similar_channels.xlsx", max_depth: int = 2):
    """
    Запускает анализ похожих каналов и сохраняет результаты в Excel-файл.
    
    Args:
        session_path: Путь к файлу сессии Telethon (.session)
        donor_channel: Идентификатор донорского канала (например, "@channel")
        output_file: Имя выходного Excel-файла
        max_depth: Максимальная глубина поиска похожих каналов (по умолчанию 2)
    
    Returns:
        dict: Словарь с данными о каналах
    """
    # Создаем папку similar, если ее нет
    os.makedirs(SIMILAR_DIR, exist_ok=True)
    
    # Если путь не начинается с SIMILAR_DIR, добавляем этот префикс
    if not output_file.startswith(SIMILAR_DIR):
        output_file = os.path.join(SIMILAR_DIR, output_file)
        
    logger.info(f"Запускаю анализ похожих каналов, начиная с {donor_channel}, глубина = {max_depth}")
    
    # Создаем клиент Telethon и подключаемся
    client = TelegramClient(session_path, **TELEGRAM_CONFIG)
    await client.connect()
    
    if not await client.is_user_authorized():
        logger.error(f"Сессия {session_path} не авторизована")
        return {}
    
    try:
        # Собираем похожие каналы
        channels_data = await collect_similar_channels(client, donor_channel, max_depth)
        logger.info(f"Найдено {len(channels_data)} каналов")
        
        # Создаем Excel-таблицу
        build_excel_table(channels_data, output_file)
        
        return channels_data
    except Exception as e:
        logger.error(f"Ошибка при анализе похожих каналов: {e}")
        return {}
    finally:
        # Закрываем соединение
        await client.disconnect()


async def check_authorized_sessions(session_files, sessions_dir):
    """
    Проверяет, какие сессии авторизованы, и возвращает список только авторизованных сессий.
    
    Args:
        session_files: Список файлов сессий (без пути)
        sessions_dir: Путь к директории с сессиями
        
    Returns:
        list: Список путей к авторизованным сессиям
    """
    authorized_sessions = []
    logger.info(f"Проверяю авторизацию {len(session_files)} сессий...")
    
    for session_file in session_files:
        session_path = os.path.join(sessions_dir, session_file)
        try:
            # Создаем временный клиент для проверки авторизации
            client = TelegramClient(session_path.replace('.session', ''), **TELEGRAM_CONFIG)
            await client.connect()
            
            if await client.is_user_authorized():
                authorized_sessions.append(session_path)
                logger.info(f"Сессия {session_file} авторизована")
            else:
                logger.warning(f"Сессия {session_file} не авторизована, исключаю из использования")
                
            await client.disconnect()
            
        except (AuthKeyDuplicatedError, PhoneNumberInvalidError) as e:
            error_type = "дублирование ключа" if isinstance(e, AuthKeyDuplicatedError) else "неверный номер"
            logger.error(f"Сессия {session_file} - ошибка авторизации: {error_type}")
        except Exception as e:
            logger.error(f"Сессия {session_file} - неизвестная ошибка: {e}")
    
    logger.info(f"Найдено {len(authorized_sessions)} авторизованных сессий из {len(session_files)}")
    return authorized_sessions


async def parallel_similar_channels_analysis(donor_channels_list: list, output_file: str = "similar_channels.xlsx", max_depth: int = 2):
    """
    Параллельно анализирует список каналов с использованием всех доступных сессий.
    
    Args:
        donor_channels_list (list): Список каналов для анализа
        output_file (str): Имя выходного файла
        max_depth (int): Максимальная глубина поиска похожих каналов
    """
    # Создаем папку similar, если ее нет
    os.makedirs(SIMILAR_DIR, exist_ok=True)
    
    # Извлекаем домен первого канала для имени файла
    if donor_channels_list:
        channel_domain = extract_channel_domain(donor_channels_list[0])
        if not output_file.startswith(SIMILAR_DIR):
            output_file = os.path.join(SIMILAR_DIR, f"{channel_domain}_similar_channels.xlsx")
        else:
            output_file = os.path.join(SIMILAR_DIR, f"{channel_domain}_similar_channels.xlsx")
    else:
        if not output_file.startswith(SIMILAR_DIR):
            output_file = os.path.join(SIMILAR_DIR, output_file)
        else:
            output_file = output_file
    
    # Получаем список всех сессий из папки sessionsPREM
    sessions_dir = "sessionsPREM"
    if not os.path.exists(sessions_dir):
        logger.error(f"Директория {sessions_dir} не существует")
        return {}
        
    session_files = [f for f in os.listdir(sessions_dir) if f.endswith('.session')]
    if not session_files:
        logger.error(f"В папке {sessions_dir} нет файлов сессий (.session)")
        return {}
    
    # Проверяем авторизацию сессий и используем только авторизованные
    sessions = await check_authorized_sessions(session_files, sessions_dir)
    
    if not sessions:
        logger.error(f"В папке {sessions_dir} нет авторизованных сессий")
        return {}
    
    session_count = len(sessions)
    logger.info(f"Найдено {session_count} авторизованных сессий в папке {sessions_dir}")
    
    # Если на одну сессию приходится слишком много каналов, предупреждаем и ограничиваем
    if session_count == 1:
        logger.warning(f"Найдена только одна авторизованная сессия. Все {len(donor_channels_list)} каналов будут обработаны через неё.")
        logger.warning(f"Ограничиваем глубину до 1 для безопасности с одной сессией.")
        print(f"ВНИМАНИЕ: Найдена только одна авторизованная сессия в папке {sessions_dir}.")
        print(f"Через неё будет обработано {len(donor_channels_list)} каналов, что повышает риск блокировки.")
        print("Для безопасности глубина поиска будет ограничена до 1.")
        
        # Для одной сессии уменьшаем глубину до 1 для безопасности
        if max_depth > 1:
            max_depth = 1
    
    elif session_count > 5:
        logger.warning(f"На каждую сессию приходится {session_count:.1f} каналов, что повышает риск блокировки.")
        print(f"ВНИМАНИЕ: На каждую сессию приходится примерно {session_count:.1f} каналов.")
        print("Это может увеличить риск блокировки аккаунтов за частые запросы.")
        print("Рекомендуется добавить больше сессий или уменьшить количество каналов.")
    
    # Распределяем каналы между сессиями
    tasks = []
    all_channels_data = {}
    
    # Если каналов больше чем сессий, распределяем равномерно
    if donor_channels_list:
        for i, donor_channel in enumerate(donor_channels_list):
            # Берем сессию по кругу (остаток от деления)
            session_index = i % len(sessions)
            session_path = sessions[session_index]
            
            # Формируем уникальное имя файла для промежуточных результатов
            temp_output = os.path.join(SIMILAR_DIR, f"temp_{os.path.basename(session_path)}_{i}.xlsx")
            
            # Создаем задачу для обработки канала
            tasks.append(
                run_similar_channels_analysis(
                    session_path=session_path,
                    donor_channel=donor_channel,
                    output_file=temp_output,
                    max_depth=max_depth
                )
            )
    else:
        # Если каналы не переданы, используем одну сессию для анализа
        logger.warning("Список каналов пуст, будет проанализирован один канал.")
        if sessions:
            tasks.append(
                run_similar_channels_analysis(
                    session_path=sessions[0],
                    donor_channel="@telegram",  # Канал по умолчанию
                    output_file=os.path.join(SIMILAR_DIR, f"temp_{os.path.basename(sessions[0])}.xlsx"),
                    max_depth=max_depth
                )
            )
    
    # Запускаем все задачи параллельно
    if tasks:
        results = await asyncio.gather(*tasks, return_exceptions=True)
        
        # Объединяем результаты
        for result in results:
            if isinstance(result, Exception):
                logger.error(f"Ошибка при анализе канала: {result}")
                continue
                
            # Объединяем словари
            for key, data in result.items():
                if key in all_channels_data:
                    # Если канал уже существует, увеличиваем счетчик пересечений
                    all_channels_data[key]['intersections'] += data['intersections']
                else:
                    # Если канала еще нет, добавляем его
                    all_channels_data[key] = data
        
        # Создаем итоговую Excel-таблицу
        build_excel_table(all_channels_data, output_file)
        
        # Удаляем временные файлы
        for task in tasks:
            if hasattr(task, 'output_file') and os.path.exists(task.output_file):
                try:
                    os.remove(task.output_file)
                except:
                    pass
                    
        logger.info(f"Анализ завершен. Найдено {len(all_channels_data)} уникальных каналов")
        return all_channels_data
    
    logger.error("Не удалось запустить ни одной задачи для анализа каналов")
    return {}


async def analyze_channel_with_all_sessions(donor_channel: str, output_file: str = "similar_channels.xlsx", max_depth: int = 2):
    """
    Анализирует один канал параллельно всеми доступными сессиями из папки sessionsPREM.
    Первая сессия получает список похожих каналов, затем этот список распределяется 
    между всеми сессиями для дальнейшей обработки.
    
    Args:
        donor_channel: Идентификатор донорского канала
        output_file: Имя выходного Excel-файла
        max_depth: Максимальная глубина поиска похожих каналов
        
    Returns:
        dict: Объединенный словарь с данными о каналах
    """
    # Создаем папку similar, если ее нет
    os.makedirs(SIMILAR_DIR, exist_ok=True)
    
    # Извлекаем домен донорского канала для имени файла
    channel_domain = extract_channel_domain(donor_channel)
    if not output_file.startswith(SIMILAR_DIR):
        output_file = os.path.join(SIMILAR_DIR, f"{channel_domain}_similar_channels.xlsx")
    else:
        output_file = os.path.join(SIMILAR_DIR, f"{channel_domain}_similar_channels.xlsx")
    
    logger.info(f"Output file will be saved to: {output_file}")
    
    # Получаем список всех сессий из папки sessionsPREM
    sessions_dir = str(BASE_DIR / "sessionsPREM")
    if not os.path.exists(sessions_dir):
        logger.error(f"Директория {sessions_dir} не существует")
        return {}
        
    session_files = [f for f in os.listdir(sessions_dir) if f.endswith('.session')]
    if not session_files:
        logger.error(f"В папке {sessions_dir} нет файлов сессий (.session)")
        return {}
    
    # Проверяем авторизацию сессий и используем только авторизованные
    sessions = await check_authorized_sessions(session_files, sessions_dir)
    
    if not sessions:
        logger.error(f"В папке {sessions_dir} нет авторизованных сессий")
        return {}
    
    logger.info(f"Найдено {len(sessions)} авторизованных сессий в папке {sessions_dir}")
    
    if len(sessions) == 1:
        logger.warning(f"Найдена только одна авторизованная сессия. Все запросы будут выполняться через неё, что увеличивает риск блокировки.")
        logger.warning(f"Ограничиваем глубину до 1 для безопасности с одной сессией.")
        print(f"ВНИМАНИЕ: Найдена только одна авторизованная сессия в папке {sessions_dir}.")
        print("Для безопасности глубина поиска будет ограничена до 1.")
        print("Рекомендуется добавить несколько сессий для распределения нагрузки.")
        
        if max_depth > 1:
            max_depth = 1
    
    # Создаем временный клиент с первой сессией
    first_session = sessions[0]
    similar_channels_data = {}  # Словарь для хранения данных о похожих каналах
    channel_titles = {}  # Словарь для хранения названий каналов
    client = None
    
    try:
        # Подключаемся к первой сессии
        client = TelegramClient(first_session, **TELEGRAM_CONFIG)
        await client.connect()
        
        if not await client.is_user_authorized():
            logger.error(f"Сессия {first_session} не авторизована")
            return {}
        
        # Получаем информацию о донорском канале
        entity = await client.get_entity(donor_channel)
        donor_key = unique_key(entity)
        
        # Инициализируем словарь с донорским каналом
        title = entity.title if hasattr(entity, 'title') else "Без названия"
        if hasattr(entity, 'username') and entity.username:
            link = f"https://t.me/{entity.username}"
        else:
            link = f"ID: {entity.id}"
        subs = getattr(entity, "participants_count", 0)
        
        similar_channels_data[donor_key] = {
            'title': title,
            'link': link,
            'subs': subs,
            'depth': 0,  # Донорский канал имеет глубину 0
            'intersections': 1,
            'second_level_donor': "",
            'donor_position': ""  # Пустая позиция для донорского канала
        }
        channel_titles[donor_key] = title
        
        # Получаем похожие каналы для донора
        logger.info(f"Получаем похожие каналы для {donor_key}...")
        await asyncio.sleep(20 * random.uniform(1, 1.2))  # Задержка перед запросом
        similar_entities = await get_similar_channels(client, donor_key)
        
        # Проверяем наличие донорского канала в списке похожих и запоминаем его позицию
        donor_position = 0
        for i, entity in enumerate(similar_entities):
            key = unique_key(entity)
            if key == donor_key:
                donor_position = i + 1  # позиция начинается с 1
                break
        
        # Если донорский канал найден в списке похожих, обновляем его данные
        if donor_position > 0:
            similar_channels_data[donor_key]['donor_position'] = donor_position
            logger.info(f"Донорский канал {title} найден в списке похожих на позиции {donor_position}")
        
        # Добавляем похожие каналы в словарь с глубиной 1
        for entity in similar_entities:
            key = unique_key(entity)
            title = entity.title if hasattr(entity, 'title') else "Без названия"
            if hasattr(entity, 'username') and entity.username:
                link = f"https://t.me/{entity.username}"
            else:
                link = f"ID: {entity.id}"
            subs = getattr(entity, "participants_count", 0)
            
            # Выводим количество подписчиков и название для каждого найденного канала
            logger.info(f"Найден канал: {title} - {subs} подписчиков - {link}")
            
            # Пропускаем каналы с менее 1000 подписчиков
            if subs < 1000:
                logger.info(f"Пропускаем канал {title} из-за малого количества подписчиков ({subs})")
                continue
            
            # Пропускаем донорский канал, т.к. он уже добавлен выше
            if key == donor_key:
                continue
                
            similar_channels_data[key] = {
                'title': title,
                'link': link,
                'subs': subs,
                'depth': 1,  # Похожие каналы имеют глубину 1
                'intersections': 1,
                'second_level_donor': "",  # У похожих на донорский канал нет донора второго уровня
                'donor_position': ""  # Будет заполнено позже, при анализе похожих каналов
            }
            channel_titles[key] = title
        
        # После получения всех похожих каналов создаем промежуточную Excel-таблицу
        # с донорским каналом и похожими на него (до поиска второго уровня)
        if len(similar_channels_data) > 1:  # Если нашли хотя бы один похожий канал
            logger.info(f"Создаем Excel-таблицу с донорским каналом и {len(similar_channels_data) - 1} похожими каналами")
            first_level_output = os.path.join(SIMILAR_DIR, f"first_level_{os.path.basename(output_file)}")
            build_excel_table(similar_channels_data, first_level_output)
            logger.info(f"Промежуточная таблица сохранена в {first_level_output}")
        
        # Список похожих каналов для распределения
        similar_channels_list = [key for key in similar_channels_data.keys() if key != donor_key]
        logger.info(f"Найдено {len(similar_channels_list)} похожих каналов для {donor_key}")
        
        # Если не найдены похожие каналы, завершаем работу без создания Excel-файла
        if len(similar_channels_list) == 0:
            logger.warning(f"Для канала {donor_channel} не найдены похожие каналы. Анализ завершен.")
            print(f"Для канала {donor_channel} не найдены похожие каналы.")
            return {}
            
        # Если глубина равна 1, то мы уже завершили работу
        if max_depth == 1:
            # Создаем Excel-таблицу
            build_excel_table(similar_channels_data, output_file)
            logger.info(f"Анализ канала {donor_channel} завершен. Найдено {len(similar_channels_data)} уникальных каналов.")
            logger.info(f"Результаты сохранены в файл: {output_file}")
            return similar_channels_data
            
        # Отключаемся от первой сессии
        if client and client.is_connected():
            await client.disconnect()
        
    except Exception as e:
        logger.error(f"Ошибка при получении похожих каналов для {donor_channel}: {e}")
        return {}
    finally:
        # Гарантированное отключение клиента в любом случае
        try:
            if client and client.is_connected():
                await client.disconnect()
                logger.info(f"Клиент для сессии {os.path.basename(first_session)} корректно отключен")
        except Exception as e:
            logger.error(f"Ошибка при отключении клиента {os.path.basename(first_session)}: {e}")
    
    # ШАГ 2: Распределяем похожие каналы между всеми сессиями для получения похожих каналов второго уровня
    logger.info(f"Шаг 2: Распределяем {len(similar_channels_list)} похожих каналов между {len(sessions)} сессиями")
    
    # Проверяем количество похожих каналов
    if not similar_channels_list:
        logger.info("Похожих каналов не найдено, завершаем выполнение")
        return {}
        
    # Создаем задачи для обработки каналов - каждая сессия обрабатывает свой набор каналов
    tasks = []
    
    # Распределяем каналы из списка похожих по сессиям
    chunks = []
    chunk_size = max(1, len(similar_channels_list) // len(sessions))
    for i in range(0, len(similar_channels_list), chunk_size):
        chunks.append(similar_channels_list[i:i + chunk_size])
    
    # Добавляем пустые чанки, если сессий больше чем чанков
    while len(chunks) < len(sessions):
        chunks.append([])
    
    await asyncio.sleep(32 * random.uniform(1, 1.2))

    # Создаем задачи для каждой сессии
    for i, (session_path, channels_chunk) in enumerate(zip(sessions, chunks)):
        if not channels_chunk:
            continue  # Пропускаем, если нет каналов для этой сессии
            
        # Имя файла без расширения
        session_name = os.path.basename(session_path).replace('.session', '')
        # Временный файл для результатов этой сессии
        temp_output = os.path.join(SIMILAR_DIR, f"temp_{session_name}.xlsx")
        
        # Создаем задачу для обработки своего набора каналов (depth=2)
        tasks.append(
            analyze_similar_channels_subset(
                session_path=session_path,
                channels_list=channels_chunk,
                donor_key=donor_key,  # Передаем ключ донора для правильной записи second_level_donor
                channel_titles=channel_titles,  # Передаем словарь с названиями каналов
                output_file=temp_output
            )
        )
    
    # Запускаем задачи и собираем результаты
    all_channels_data = similar_channels_data.copy()  # Начинаем с уже найденных каналов
    
    if tasks:
        results = await asyncio.gather(*tasks, return_exceptions=True)
        
        # Объединяем результаты
        for i, result in enumerate(results):
            if isinstance(result, Exception):
                logger.error(f"Ошибка при анализе группы каналов с сессией {session_files[i]}: {result}")
                continue
                
            # Объединяем словари
            for key, data in result.items():
                if key in all_channels_data:
                    # Если канал уже существует, увеличиваем счетчик пересечений
                    all_channels_data[key]['intersections'] += data['intersections']
                    
                    # Обновляем информацию о доноре второго уровня, если она есть и поле пустое
                    if data['second_level_donor'] and not all_channels_data[key]['second_level_donor']:
                        all_channels_data[key]['second_level_donor'] = data['second_level_donor']
                    
                    # Обновляем позицию донора, если она есть и поле пустое
                    if data.get('donor_position') and not all_channels_data[key].get('donor_position'):
                        all_channels_data[key]['donor_position'] = data['donor_position']
                else:
                    # Если канала еще нет, добавляем его
                    all_channels_data[key] = data
        
        # Создаем итоговую Excel-таблицу
        build_excel_table(all_channels_data, output_file)
        logger.info(f"Итоговая таблица сохранена в {output_file}")
        
        # Удаляем временные файлы
        for i, task in enumerate(tasks):
            session_name = os.path.basename(sessions[i]).replace('.session', '')
            temp_output = os.path.join(SIMILAR_DIR, f"temp_{session_name}.xlsx")
            if os.path.exists(temp_output):
                try:
                    os.remove(temp_output)
                    logger.info(f"Удален временный файл {temp_output}")
                except Exception as e:
                    logger.error(f"Не удалось удалить временный файл {temp_output}: {e}")
                    
        logger.info(f"Анализ канала {donor_channel} завершен. Найдено {len(all_channels_data)} уникальных каналов.")
        return all_channels_data
    
    # Если задачи не были созданы, возвращаем только данные о донорском канале и похожих каналах
    logger.warning("Не удалось запустить задачи для анализа похожих каналов")
    build_excel_table(similar_channels_data, output_file)
    logger.info(f"Таблица сохранена в {output_file}")
    return similar_channels_data


async def analyze_similar_channels_subset(session_path: str, channels_list: list, donor_key: str, channel_titles: dict, output_file: str = "temp_channels.xlsx"):
    """
    Анализирует подмножество похожих каналов (получает похожие каналы второго уровня).
    
    Args:
        session_path: Путь к файлу сессии
        channels_list: Список каналов для анализа
        donor_key: Ключ донорского канала
        channel_titles: Словарь с названиями каналов
        output_file: Имя выходного файла
        
    Returns:
        dict: Словарь с данными о каналах
    """
    # Создаем папку similar, если ее нет
    os.makedirs(SIMILAR_DIR, exist_ok=True)
    
    # Если путь не начинается с SIMILAR_DIR, добавляем этот префикс
    if not output_file.startswith(SIMILAR_DIR):
        output_file = os.path.join(SIMILAR_DIR, output_file)
        
    logger.info(f"Анализирую {len(channels_list)} каналов с сессией {os.path.basename(session_path)}")
    
    # Создаем клиент Telethon
    client = None
    
    try:
        client = TelegramClient(session_path, **TELEGRAM_CONFIG)
        await client.connect()
        
        if not await client.is_user_authorized():
            logger.error(f"Сессия {session_path} не авторизована")
            return {}
        
        # Словарь для хранения результатов
        channels_data = {}
        
        # Обрабатываем каналы по очереди
        for i, channel_key in enumerate(channels_list):
            # Добавляем случайную задержку между запросами
            if i > 0:
                delay = 32 * random.uniform(1, 1.2)  # 60 секунд между запросами
                logger.info(f"Делаю паузу {delay:.1f} секунд перед запросом канала {channel_key}")
                await asyncio.sleep(delay)
                
            try:
                # Получаем похожие каналы для текущего канала
                logger.info(f"Получаю похожие каналы для {channel_key}")
                
                similar_entities = await get_similar_channels(client, channel_key)
                logger.info(f"Найдено {len(similar_entities)} похожих каналов для {channel_key}")
                
                # Проверяем, присутствует ли донорский канал в списке похожих, и если да, запоминаем его позицию
                donor_position = 0
                for i, entity in enumerate(similar_entities):
                    if unique_key(entity) == donor_key:
                        donor_position = i + 1  # Позиция начинается с 1
                        break
                
                # Обрабатываем похожие каналы
                for sim_entity in similar_entities:
                    sim_key = unique_key(sim_entity)
                    
                    # Пропускаем донорский канал
                    if sim_key == donor_key:
                        continue
                        
                    # Увеличиваем счетчик пересечений, если канал уже есть
                    if sim_key in channels_data:
                        channels_data[sim_key]['intersections'] += 1
                        continue
                    
                    # Добавляем новый канал
                    title = sim_entity.title if hasattr(sim_entity, 'title') else "Без названия"
                    if hasattr(sim_entity, 'username') and sim_entity.username:
                        link = f"https://t.me/{sim_entity.username}"
                    else:
                        link = f"ID: {sim_entity.id}"
                    subs = getattr(sim_entity, "participants_count", 0)
                    
                    if subs < 1000:
                        # logger.info(f"Пропускаем канал {title} из-за малого количества подписчиков ({subs})")
                        continue
                
                    channels_data[sim_key] = {
                        'title': title,
                        'link': link,
                        'subs': subs,
                        'depth': 2,  # Это каналы глубины 2
                        'intersections': 1,
                        'second_level_donor': channel_titles.get(channel_key, ""),  # Берем название канала-донора из словаря
                        'donor_position': ""  # По умолчанию пусто
                    }
                
                # Если донорский канал найден в списке похожих, сохраняем его позицию для текущего канала
                if donor_position > 0 and channel_key in channels_data:
                    channels_data[channel_key]['donor_position'] = donor_position
                    logger.info(f"Донорский канал найден на позиции {donor_position} в списке похожих для {channel_titles.get(channel_key, channel_key)}")
                    
            except Exception as e:
                logger.error(f"Ошибка при получении похожих каналов для {channel_key}: {e}")
                continue
        
        # Сохраняем результаты во временный файл
        if channels_data:
            build_excel_table(channels_data, output_file)
            
        logger.info(f"Анализ подмножества из {len(channels_list)} каналов завершен. Найдено {len(channels_data)} уникальных каналов второго уровня.")
        return channels_data
    except Exception as e:
        logger.error(f"Общая ошибка в analyze_similar_channels_subset: {e}")
        return {}
    finally:
        # Корректное закрытие клиента, даже при ошибках
        try:
            if client and client.is_connected():
                await client.disconnect()
                logger.info(f"Клиент для сессии {os.path.basename(session_path)} корректно отключен")
        except Exception as e:
            logger.error(f"Ошибка при отключении клиента {os.path.basename(session_path)}: {e}")


async def convertToTDATA(session: str):
    """
    Конвертирует сессию Telethon в формат TDesktop (tdata).
    
    Args:
        session: Имя файла сессии (без пути)
    """
    if not OPENTELE_AVAILABLE:
        logger.error("Невозможно выполнить конвертацию: библиотека opentele не установлена.")
        print("Ошибка: Для конвертации сессий установите библиотеку opentele:")
        print("pip install opentele")
        return False
        
    try:
        # Создаем директорию для сохранения tdata, если её нет
        os.makedirs("tdata", exist_ok=True)
        
        session_path = os.path.join("sessions", session)
        if not os.path.exists(session_path + ".session"):
            logger.error(f"Файл сессии {session_path}.session не найден")
            return False
            
        # Загружаем клиента Telethon из файла сессии
        client = TelegramClient(session_path, **TELEGRAM_CONFIG)
        await client.connect()
        
        if not await client.is_user_authorized():
            logger.error(f"Сессия {session} не авторизована")
            await client.disconnect()
            return False
            
        # Конвертируем в TDesktop
        logger.info(f"Конвертирую сессию {session} в формат TDesktop...")
        tdesk = await client.ToTDesktop(flag=UseCurrentSession)
        
        # Создаем уникальную папку для каждой сессии
        tdata_folder = os.path.join("tdata", session)
        os.makedirs(tdata_folder, exist_ok=True)
        
        # Сохраняем TDesktop-сессию
        tdesk.SaveTData(tdata_folder)
        logger.info(f"Сессия {session} успешно конвертирована в tdata: {tdata_folder}")
        print(f"Сессия {session} успешно конвертирована в tdata: {tdata_folder}")
        
        await client.disconnect()
        return True
    except Exception as e:
        logger.error(f"Ошибка при конвертации сессии {session}: {e}")
        print(f"Ошибка при конвертации сессии {session}: {e}")
        return False


async def convert_sessions_to_tdata(sessions_list=None):
    """
    Конвертирует список сессий из формата Telethon в формат TDesktop.
    Если список не указан, конвертирует все сессии из папки sessions.
    
    Args:
        sessions_list: Список имен файлов сессий для конвертации (без расширения .session)
        
    Returns:
        tuple: (количество успешных конвертаций, количество ошибок)
    """
    if not OPENTELE_AVAILABLE:
        logger.error("Невозможно выполнить конвертацию: библиотека opentele не установлена.")
        print("Ошибка: Для конвертации сессий установите библиотеку opentele:")
        print("pip install opentele")
        return 0, 0
        
    # Если список не указан, берем все файлы из директории sessions
    if not sessions_list:
        if not os.path.exists("sessions"):
            logger.error("Директория 'sessions' не найдена")
            return 0, 0
            
        sessions_list = [f[:-8] for f in os.listdir("sessions") if f.endswith(".session")]
        
    if not sessions_list:
        logger.warning("Не найдено сессий для конвертации")
        return 0, 0
        
    success_count = 0
    error_count = 0
    
    logger.info(f"Начинаю конвертацию {len(sessions_list)} сессий...")
    print(f"Начинаю конвертацию {len(sessions_list)} сессий...")
    
    for session in sessions_list:
        print(f"Конвертирую сессию: {session}")
        if await convertToTDATA(session):
            success_count += 1
        else:
            error_count += 1
            
    logger.info(f"Конвертация завершена. Успешно: {success_count}, ошибок: {error_count}")
    print(f"Конвертация завершена. Успешно: {success_count}, ошибок: {error_count}")
    
    return success_count, error_count


async def run_channels_analysis_cli():
    """
    Консольный интерфейс для запуска анализа похожих каналов.
    Позволяет выбрать режим работы и указать параметры.
    """
    parser = argparse.ArgumentParser(description='Анализ похожих Telegram-каналов')
    parser.add_argument('--mode', type=str, choices=['single', 'multi', 'convert'], default='single',
                      help='Режим работы: single - один канал, multi - список каналов, convert - конвертация сессий в tdata')
    parser.add_argument('--channel', type=str, help='Канал для анализа (для режима single)')
    parser.add_argument('--channels-file', type=str, help='Путь к файлу со списком каналов (для режима multi)')
    parser.add_argument('--output', type=str, default='similar_channels.xlsx',
                      help='Имя выходного Excel-файла')
    parser.add_argument('--depth', type=int, default=2,
                      help='Глубина поиска похожих каналов (по умолчанию 2)')
    parser.add_argument('--sessions', type=str, help='Список сессий через запятую для конвертации (для режима convert)')
    
    args = parser.parse_args()
    
    if args.mode == 'convert':
        if args.sessions:
            sessions_list = [s.strip() for s in args.sessions.split(',')]
            await convert_sessions_to_tdata(sessions_list)
        else:
            await convert_sessions_to_tdata()  # Конвертировать все сессии
        return
    
    if args.mode == 'single':
        if not args.channel:
            print("Ошибка: для режима 'single' необходимо указать канал через параметр --channel")
            return
            
        logger.info(f"2Запускаю анализ канала {args.channel} с глубиной {args.depth}")
        await analyze_channel_with_all_sessions(
            donor_channel=args.channel,
            output_file=args.output,
            max_depth=args.depth
        )
    else:  # multi
        if not args.channels_file:
            print("Ошибка: для режима 'multi' необходимо указать файл со списком каналов через параметр --channels-file")
            return
            
        try:
            with open(args.channels_file, 'r') as f:
                channels = [line.strip() for line in f if line.strip()]
                
            if not channels:
                print(f"Ошибка: файл {args.channels_file} пуст или не содержит валидных каналов")
                return
                
            logger.info(f"Запускаю анализ {len(channels)} каналов с глубиной {args.depth}")
            await parallel_similar_channels_analysis(
                donor_channels_list=channels,
                output_file=args.output,
                max_depth=args.depth
            )
        except Exception as e:
            logger.error(f"Ошибка при чтении файла со списком каналов: {e}")


async def close_all_pending_tasks():
    """
    Закрывает все незавершенные задачи для корректного завершения программы.
    Это предотвращает ошибки "Task was destroyed but it is pending!"
    """
    tasks = [task for task in asyncio.all_tasks() if task is not asyncio.current_task()]
    if not tasks:
        return
        
    logger.info(f"Завершаю {len(tasks)} незавершенных задач...")
    for task in tasks:
        task.cancel()
        
    await asyncio.gather(*tasks, return_exceptions=True)
    logger.info("Все задачи успешно завершены")


async def run_with_proper_shutdown(coro):
    """
    Запускает корутину с корректной обработкой сигналов завершения
    и закрытием всех задач.
    
    Args:
        coro: Корутина для запуска
    """
    try:
        await coro
    finally:
        await close_all_pending_tasks()


def extract_channel_domain(channel: str) -> str:
    """
    Извлекает домен канала из различных форматов ввода.
    
    Args:
        channel: Строка с идентификатором канала (может быть в форматах @channel, https://t.me/channel, channel)
        
    Returns:
        str: Домен канала
    """
    # Убираем @ если есть
    channel = channel.lstrip('@')
    
    # Если это полный URL, извлекаем домен
    if 't.me/' in channel:
        channel = channel.split('t.me/')[-1]
    
    # Убираем любые оставшиеся параметры после /
    channel = channel.split('/')[0]
    
    return channel


if __name__ == "__main__":
    # Запуск из командной строки с корректной обработкой завершения
    asyncio.run(run_with_proper_shutdown(run_channels_analysis_cli())) 