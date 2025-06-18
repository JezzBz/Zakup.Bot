#!/bin/bash

echo "🚀 Запуск ZakupBot в Docker..."

# Проверяем наличие Docker
if ! command -v docker &> /dev/null; then
    echo "❌ Docker не установлен. Установите Docker и попробуйте снова."
    exit 1
fi

# Проверяем наличие Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose не установлен. Установите Docker Compose и попробуйте снова."
    exit 1
fi

# Создаем необходимые директории
echo "📁 Создание директорий..."
mkdir -p tg/sessionsPREM tg/similar

# Останавливаем существующие контейнеры
echo "🛑 Остановка существующих контейнеров..."
docker-compose down

# Собираем и запускаем сервисы
echo "🔨 Сборка и запуск сервисов..."
docker-compose up -d --build

# Ждем немного для запуска
echo "⏳ Ожидание запуска сервисов..."
sleep 10

# Проверяем статус
echo "📊 Статус сервисов:"
docker-compose ps

echo ""
echo "✅ ZakupBot запущен!"
echo ""
echo "🌐 Доступные сервисы:"
echo "   • Zakup API: http://localhost:8080"
echo "   • Telegram Analyzer: http://localhost:8000"
echo "   • MinIO Console: http://localhost:9001"
echo "   • PostgreSQL: localhost:5432"
echo ""
echo "📋 Полезные команды:"
echo "   • Просмотр логов: docker-compose logs -f"
echo "   • Остановка: docker-compose down"
echo "   • Перезапуск: docker-compose restart" 