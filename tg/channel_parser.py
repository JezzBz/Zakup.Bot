import re
from loguru import logger

class TelegramChannel:
    """
    Класс для работы с каналами Telegram.
    Обрабатывает различные форматы ссылок на каналы и группы.
    """
    def __init__(self, channel_name: str):
        self.channel_name = channel_name

    def is_private(self) -> bool:
        """Проверяет, является ли канал приватным"""
        return "t.me/joinchat/" in self.channel_name

    def formatted(self) -> str:
        """Возвращает отформатированное имя канала для использования в API-запросах"""
        if self.is_private():
            return self.channel_name.split("t.me/joinchat/")[-1] 
        elif "@" in self.channel_name:
            return self.channel_name.split("@")[-1]
        elif "+" in self.channel_name:
            return self.channel_name.split("+")[-1]
        elif "t.me/" in self.channel_name:
            return self.channel_name.split("t.me/")[-1]
        return self.channel_name

    def get_full_url(self) -> str:
        """Возвращает полный URL канала"""
        if "t.me/" in self.channel_name:
            return self.channel_name
        elif "@" in self.channel_name:
            username = self.channel_name.split("@")[-1]
            return f"https://t.me/{username}"
        return f"https://t.me/{self.channel_name}"
        
    def get_open_url(self) -> str:
        """Возвращает URL для открытого канала (с префиксом /s/)"""
        if self.is_private():
            return None
        else:
            base_url = self.get_full_url()
            return base_url.replace("t.me/", "t.me/s/")

    def __str__(self) -> str:
        return self.channel_name
    
    def __eq__(self, other):
        if isinstance(other, TelegramChannel):
            return self.channel_name == other.channel_name
        return False
    
    def __hash__(self):
        return hash(self.channel_name)


class ChannelParser:
    """
    Класс для парсинга файлов с каналами.
    """
    def __init__(self, file_path: str):
        self.file_path = file_path

    def parse(self):
        """Парсит файл и возвращает список объектов TelegramChannel"""
        with open(self.file_path, "r", encoding="utf-8") as f:
            return [TelegramChannel(line.strip()) for line in f if line.strip()]
            
    @staticmethod
    def split_channels(channels, n):
        """Разделяет список каналов на n частей."""
        avg = len(channels) // n
        remainder = len(channels) % n
        splitted_channels = []
        i = 0
        for _ in range(n):
            end = i + avg + (1 if remainder > 0 else 0)
            splitted_channels.append(channels[i:end])
            i = end
            remainder -= 1
        return splitted_channels 