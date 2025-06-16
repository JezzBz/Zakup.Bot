import os
import shutil
import asyncio
import random
import csv
import sqlite3
from datetime import datetime
from loguru import logger
from functools import wraps

from telethon import TelegramClient
from telethon.errors import (
    AuthKeyDuplicatedError,
    PhoneNumberInvalidError,
    FloodWaitError
)

from config import TELEGRAM_CONFIG, SESSIONS_PATH


def handle_flood_wait():
    """
    Декоратор для обработки FloodWaitError.
    Повторяет запрос после ожидания указанного времени.
    """
    def decorator(func):
        async def wrapper(*args, **kwargs):
            while True:
                try:
                    return await func(*args, **kwargs)
                except FloodWaitError as e:
                    logger.error(f"FloodWaitError: ожидание {e.seconds} секунд")
                    await asyncio.sleep(e.seconds + 85)
        return wrapper
    return decorator


def check_authorization():
    """
    Декоратор для проверки авторизации клиента.
    В случае ошибки авторизации обрабатывает сессию и возвращает None.
    """
    def decorator(func):
        @wraps(func)
        async def wrapper(self, session: str, *args, **kwargs):
            client = TelegramClient(os.path.join("sessions", session), **TELEGRAM_CONFIG)
            try:
                await client.connect()
                logger.info(f"{client.session.filename} - Запрос к API: connect")
                
                if not await client.is_user_authorized():
                    logger.error(f"{client.session.filename} - не авторизован")
                    await handle_un_auth_error(session, self.sessions)
                    move_dead_session(session, SESSIONS_PATH)
                    return None
                
                logger.info(f"{client.session.filename} - Авторизован")
                await asyncio.sleep(25)  # Сохраняем задержку
                
                # Передаем клиент в оригинальную функцию вместо session
                return await func(self, client, *args, **kwargs)
                
            except (AuthKeyDuplicatedError, PhoneNumberInvalidError) as e:
                error_type = "дублирование ключа" if isinstance(e, AuthKeyDuplicatedError) else "неверный номер"
                logger.error(f"{client.session.filename} - Ошибка авторизации: {error_type}")
                await handle_un_auth_error(session, self.sessions)
                move_dead_session(session, SESSIONS_PATH)
                return None
        return wrapper
    return decorator


def move_dead_session(session: str, sessions_path: str, dead_sessions_folder: str = "sessionsDied"):
    """
    Перемещает файл сессии в папку 'sessionsDied'.

    Args:
        session: Имя файла сессии (без пути)
        sessions_path: Директория, где хранятся активные сессии
        dead_sessions_folder: Директория, куда перемещаются мёртвые сессии
    """
    os.makedirs(dead_sessions_folder, exist_ok=True)

    session_file = os.path.join(sessions_path, session)
    destination_file = os.path.join(dead_sessions_folder, session)

    logger.info(f"Попытка переместить файл сессии: {session_file} -> {destination_file}")

    if os.path.exists(session_file):
        try:
            shutil.move(session_file, destination_file)
            logger.info(f"Файл сессии {session} перемещён в {dead_sessions_folder}")
        except Exception as e:
            logger.error(f"Ошибка перемещения файла сессии {session}: {e}")
    else:
        logger.warning(f"Файл {session_file} не найден. Возможно, он уже удалён.")


async def handle_un_auth_error(session: str, sessions_list: list):
    """
    Обрабатывает неавторизованные сессии.
    Записывает информацию о сессии и перераспределяет каналы.

    Args:
        session: Имя сессии
        sessions_list: Список всех сессий
    """
    logger.error(f"Ошибка авторизации сессии {session}")
    with sqlite3.connect("sessions.db") as conn:
        cursor = conn.cursor()
        cursor.execute("SELECT * FROM sessions WHERE session_name = ?", (session,))
        session_info = cursor.fetchone()

    if session_info:
        session_name, start_time, request_count = session_info
        duration = datetime.now() - datetime.strptime(start_time, "%Y-%m-%d %H:%M:%S")

        parts = session.split("_")
        seller = parts[0]
        country = parts[1] if len(parts) > 1 else ""
        index = parts[2] if len(parts) > 2 else ""
        price = parts[-1]

        with open("session_info.csv", "a", newline="", encoding="utf-8") as csvfile:
            fieldnames = ["seller", "country", "index", "price", "duration", "request_count"]
            writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
            writer.writerow({
                "seller": seller,
                "country": country,
                "index": index,
                "price": price,
                "duration": duration.total_seconds() / 3600,
                "request_count": request_count,
            })

    with sqlite3.connect("telegram_bot.db") as conn:
        cursor = conn.cursor()
        cursor.execute("SELECT channel FROM accounts WHERE account = ?", (session,))
        channels_for_session = [row[0] for row in cursor.fetchall()]
        cursor.execute("DELETE FROM accounts WHERE account = ?", (session,))
        conn.commit()

    if not channels_for_session:
        logger.info(f"Для сессии {session} не найдено каналов для перераспределения.")
        return

    logger.info(f"Сессия {session} заблокирована. Перераспределяем {len(channels_for_session)} каналов.")
    active_sessions = [s for s in sessions_list if s != session]
    if not active_sessions:
        logger.error("Нет доступных сессий для перераспределения каналов!")
        return

    with sqlite3.connect("telegram_bot.db") as conn:
        cursor = conn.cursor()
        conn.execute("BEGIN")
        for i, channel in enumerate(channels_for_session):
            target_session = active_sessions[i % len(active_sessions)]
            try:
                cursor.execute("INSERT INTO accounts (account, channel) VALUES (?, ?)", (target_session, channel))
            except sqlite3.IntegrityError:
                logger.warning(f"Канал {channel} уже распределён, пропускаем")
        conn.commit()
    logger.info("Каналы успешно перераспределены между оставшимися сессиями.")


def calculate_delay(account_index, total_accounts):
    """
    Рассчитывает задержку для входа аккаунта в канал.
    
    Args:
        account_index: Индекс аккаунта
        total_accounts: Общее количество аккаунтов
    
    Returns:
        float: Задержка в секундах
    """
    # 60% аккаунтов вступают в первые 10 минут
    if account_index < total_accounts * 0.6:
        return random.uniform(0, 10 * 60)
    # 30% аккаунтов в следующие 20 минут
    elif account_index < total_accounts * 0.9:
        return random.uniform(10 * 60, 30 * 60)
    # 10% аккаунтов в течении следующего часа
    else:
        return random.uniform(30 * 60, 90 * 60)


class SessionManager:
    """
    Класс для управления сессиями Telegram.
    Загружает сессии, проверяет их авторизацию и выполняет действия с ними.
    """
    def __init__(self, sessions_path=SESSIONS_PATH):
        self.sessions_path = sessions_path
        self.sessions = self._load_sessions()
        self.request_counts = self._initialize_request_counts()
        
    def _load_sessions(self):
        """Загружает список сессий из директории"""
        return [f for f in os.listdir(self.sessions_path) if f.endswith(".session")]
        
    def _initialize_request_counts(self):
        """Инициализирует счетчики запросов для сессий"""
        request_counts = {}
        try:
            with sqlite3.connect("sessions.db") as conn:
                cursor = conn.cursor()
                cursor.execute("SELECT session_name, request_count FROM sessions")
                for session_name, count in cursor.fetchall():
                    request_counts[session_name] = count
        except Exception as e:
            logger.error(f"Ошибка инициализации счетчиков запросов: {e}")
        return request_counts
            
    async def increment_request_count(self, session):
        """
        Увеличивает счетчик запросов для сессии и обновляет БД.
        
        Args:
            session: Имя сессии
        """
        self.request_counts[session] = self.request_counts.get(session, 0) + 1
        if self.request_counts[session] % 10 == 0:
            try:
                with sqlite3.connect("sessions.db") as conn:
                    conn.execute("UPDATE sessions SET request_count = ? WHERE session_name = ?", 
                                (self.request_counts[session], session))
                    conn.commit()
            except Exception as e:
                logger.error(f"Ошибка обновления счетчика запросов для {session}: {e}")
    
    async def get_client(self, session):
        """
        Создает и возвращает клиент Telegram.
        
        Args:
            session: Имя сессии
            
        Returns:
            TelegramClient или None, если авторизация не удалась
        """
        client = TelegramClient(os.path.join(self.sessions_path, session), **TELEGRAM_CONFIG)
        try:
            await client.connect()
            logger.info(f"{session} - Запрос к API: connect")
            
            if not await client.is_user_authorized():
                logger.error(f"{session} - не авторизован")
                await handle_un_auth_error(session, self.sessions)
                move_dead_session(session, self.sessions_path)
                await client.disconnect()
                return None
            
            logger.info(f"{session} - Авторизован")
            await self.increment_request_count(session)
            return client
        except (AuthKeyDuplicatedError, PhoneNumberInvalidError) as e:
            error_type = "дублирование ключа" if isinstance(e, AuthKeyDuplicatedError) else "неверный номер"
            logger.error(f"{session} - Ошибка авторизации: {error_type}")
            await handle_un_auth_error(session, self.sessions)
            move_dead_session(session, self.sessions_path)
            await client.disconnect()
            return None
        except Exception as e:
            logger.error(f"{session} - Непредвиденная ошибка: {e}")
            await client.disconnect()
            return None
    
    async def get_all_accounts_info(self):
        """
        Получает информацию обо всех аккаунтах.
        
        Returns:
            list: Список словарей с информацией об аккаунтах
        """
        results = []
        for session in self.sessions:
            client = await self.get_client(session)
            if not client:
                continue
                
            try:
                me = await client.get_me()
                results.append({
                    "session": session,
                    "first_name": me.first_name,
                    "last_name": me.last_name,
                    "username": me.username,
                    "phone": me.phone,
                    "requests": self.request_counts.get(session, 0)
                })
            except Exception as e:
                logger.error(f"Ошибка получения информации об аккаунте {session}: {e}")
            finally:
                await client.disconnect()
                
        return results 