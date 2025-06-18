FROM python:3.11-slim

WORKDIR /app

# Установка минимально необходимых системных зависимостей для PyQt5
RUN apt-get update && apt-get install -y \
    build-essential \
    libssl-dev \
    libglib2.0-0 \
    libxcb1 \
    libxcb-render0 \
    libxcb-shape0 \
    libxcb-xfixes0 \
    libxcb-keysyms1 \
    libxcb-icccm4 \
    libxcb-image0 \
    libxcb-shm0 \
    libxcb-util1 \
    libxcb-randr0 \
    libxcb-xinerama0 \
    libxkbcommon-x11-0 \
    libxrender1 \
    libxext6 \
    libx11-6 \
    libxfixes3 \
    libxrandr2 \
    libxss1 \
    libxtst6 \
    libxi6 \
    libxcursor1 \
    libxcomposite1 \
    libxdamage1 \
    libasound2 \
    libpulse0 \
    libdbus-1-3 \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libpng16-16 \
    libjpeg62-turbo \
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