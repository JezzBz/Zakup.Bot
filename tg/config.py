# config.py
import configparser
import random
import os

# Загрузка конфигурации
CONFIG = configparser.ConfigParser()
config_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "config.ini")
CONFIG.read(config_path)

# Общие настройки для Telegram
TELEGRAM_CONFIG = {
    "api_id": 2040,
    "api_hash": "b18441a1ff607e10a989891a5462e627",
    "device_model": "Desktop",
    "system_version": "Windows 10",
    "app_version": "3.4.3 x64",
    "lang_code": "en",
    "system_lang_code": "en-US",
}

# Пути к файлам
SESSIONS_PATH = "./sessions"

# Константы из bot.py
CHANNELS_PATH = CONFIG["configuration"].get("channels_path", "./channels.txt")
HTTP_SEMAPHORE_LIMIT = 60
TELETHON_SEMAPHORE_LIMIT = 20


# Константы для выполнения
SLEEP_TIME = int(CONFIG["configuration"].get("sleep_time", "30"))
RANDOM_SLEEP_FACTOR_MIN = 1
RANDOM_SLEEP_FACTOR_MAX = 1.2
VALID_SCRIPT_TYPES = ["getSimilar"]

# Вспомогательные функции
def get_random_sleep_time():
    """Возвращает случайное время сна, умноженное на SLEEP_TIME"""
    return SLEEP_TIME * random.uniform(RANDOM_SLEEP_FACTOR_MIN, RANDOM_SLEEP_FACTOR_MAX)
