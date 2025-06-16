#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Скрипт для запуска анализа похожих Telegram-каналов с использованием сессий из папки sessionsPREM.
Также позволяет конвертировать сессии Telethon в формат TDesktop (tdata).
"""

import asyncio
import argparse
import os
import pathlib
import sys
import time
from datetime import datetime
from typing import List, Optional

import config
from loguru import logger
from telethon import TelegramClient
from telethon.tl.functions.channels import GetFullChannelRequest
from telethon.tl.types import Channel, User

from telegram_manager import (
    analyze_channel_with_all_sessions, 
    parallel_similar_channels_analysis,
    convert_sessions_to_tdata
)

# Определяем базовую директорию относительно расположения скрипта
base_dir = pathlib.Path(__file__).parent

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
parser.add_argument('--similar-dir', type=str, default=str(base_dir / "similar"),
                  help='Директория для сохранения результатов')
parser.add_argument('--user-id', type=int, default=0,
                  help='ID пользователя для логирования')

args = parser.parse_args()

# Создаем директорию для результатов
SIMILAR_DIR = pathlib.Path(args.similar_dir)
SIMILAR_DIR.mkdir(parents=True, exist_ok=True)

# sessions лежат внутри tg
SESSIONS_DIR = base_dir / "sessionsPREM"
session_files = [f for f in os.listdir(SESSIONS_DIR) if f.endswith(".session")]

async def main():
    # Режим конвертации сессий
    if args.mode == 'convert':
        try:
            # Проверяем директорию с сессиями
            if not os.path.exists(args.sessions_dir):
                logger.error(f"Директория {args.sessions_dir} не найдена.")
                print(f"Ошибка: Директория '{args.sessions_dir}' не найдена.")
                return
                
            # Если указан список сессий, конвертируем только их
            if args.sessions:
                sessions_list = [s.strip() for s in args.sessions.split(',')]
                print(f"Режим конвертации сессий: {', '.join(sessions_list)}")
                await convert_sessions_to_tdata(sessions_list)
            # Иначе конвертируем все сессии из директории
            else:
                session_files = [f[:-8] for f in os.listdir(args.sessions_dir) if f.endswith('.session')]
                if not session_files:
                    print(f"В директории {args.sessions_dir} не найдено файлов сессий (.session)")
                    return
                print(f"Начинаю конвертацию всех сессий из директории {args.sessions_dir}")
                await convert_sessions_to_tdata(session_files)
            return
        except Exception as e:
            logger.error(f"Ошибка при конвертации сессий: {e}")
            print(f"Ошибка при конвертации сессий: {e}")
            return
    
    # Режимы анализа каналов
    # Проверяем наличие папки sessionsPREM
    if not os.path.exists('./tg/sessionsPREM'):
        logger.error("Папка sessionsPREM не найдена.")
        print("Ошибка: Папка 'sessionsPREM' не найдена в текущей директории.")
        return
    
    # Проверяем наличие сессий в папке
    session_files = [f for f in os.listdir('./tg/sessionsPREM') if f.endswith('.session')]
    if not session_files:
        logger.error("В папке sessionsPREM нет файлов .session.")
        print("Ошибка: В папке 'sessionsPREM' нет файлов сессий (.session).")
        return
    
    logger.info(f"Найдено {len(session_files)} сессий в папке sessionsPREM")
    print(f"Найдено {len(session_files)} сессий в папке sessionsPREM")
    
    # Предупреждение о возможных рисках
    if len(session_files) == 1:
        print("\n⚠️  ВНИМАНИЕ: Обнаружена только одна сессия Telegram!")
        print("Это существенно увеличивает нагрузку на аккаунт и может привести к блокировке.")
        print("Рекомендуется добавить несколько сессий для распределения запросов.")
        
        if not args.force_depth and args.depth > 1:
            print(f"\nДля безопасности глубина поиска будет снижена до 1 (вместо {args.depth}).")
            print("Если вы хотите использовать указанную глубину на свой страх и риск,")
            print("добавьте флаг --force-depth к команде.")
            
            if not input("\nПродолжить с пониженной глубиной? (д/н): ").lower().startswith('д'):
                print("Операция отменена.")
                return
    
    if args.mode == 'single':
        if not args.channel:
            print("Ошибка: для режима 'single' необходимо указать канал через параметр --channel")
            return
            
        # Формируем имя файла с учетом ID пользователя
        channel_domain = args.channel.replace('@', '').replace('https://t.me/', '')
        output_file = args.output or f'similar/{channel_domain}_similar_channels_{args.user_id}.xlsx'
        
        logger.info(f"Запускаю анализ канала {args.channel} с глубиной {args.depth}")
        print(f"1Запускаю анализ канала {args.channel} с глубиной {args.depth}...")
        result = await analyze_channel_with_all_sessions(
            donor_channel=args.channel,
            output_file=output_file,
            max_depth=args.depth
        )
        
        if not result:
            print(f"Анализ завершен. Для канала {args.channel} не найдено похожих каналов.")
            return
            
        print(f"Анализ завершен! Найдено {len(result)} уникальных каналов.")
        print(f"Результат сохранен в файл: {output_file}")
        
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
                
            # Предупреждение если слишком много каналов на одну сессию
            channels_per_session = len(channels) / len(session_files)
            if channels_per_session > 5:
                print(f"\n⚠️  ВНИМАНИЕ: На каждую сессию приходится {channels_per_session:.1f} каналов!")
                print("Это может привести к блокировке аккаунтов из-за слишком частых запросов.")
                print("Рекомендуется добавить больше сессий или уменьшить количество каналов.")
                
                if not input("\nПродолжить выполнение? (д/н): ").lower().startswith('д'):
                    print("Операция отменена.")
                    return
                
            # Формируем имя файла с учетом ID пользователя
            output_file = args.output or f'similar/multiple_channels_{args.user_id}.xlsx'
            
            logger.info(f"Запускаю анализ {len(channels)} каналов с глубиной {args.depth}")
            print(f"Запускаю анализ {len(channels)} каналов с глубиной {args.depth}...")
            result = await parallel_similar_channels_analysis(
                donor_channels_list=channels,
                output_file=output_file,
                max_depth=args.depth
            )
            print(f"Анализ завершен! Найдено {len(result)} уникальных каналов.")
            print(f"Результат сохранен в файл: {output_file}")
            
        except Exception as e:
            logger.error(f"Ошибка при чтении файла со списком каналов: {e}")
            print(f"Ошибка при чтении файла со списком каналов: {e}")

if __name__ == "__main__":
    # Настраиваем логгер
    # logger.add("analyze_channels.log", rotation="10 MB", level="INFO")
    
    # Запускаем
    asyncio.run(main()) 