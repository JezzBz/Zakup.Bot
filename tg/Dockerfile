FROM python:3.11-slim

WORKDIR /app

# Установка необходимых системных зависимостей
RUN apt-get update && apt-get install -y \
    build-essential \
    libssl-dev \
    && rm -rf /var/lib/apt/lists/*

# Копирование файлов зависимостей
COPY requirements.txt .

# Установка Python зависимостей
RUN pip install --no-cache-dir -r requirements.txt

# Копирование исходного кода
COPY . .

# Создание необходимых директорий
RUN mkdir -p similar sessionsPREM

# Открытие порта
EXPOSE 8000

# Запуск приложения
CMD ["python", "api_server.py"] 