#!/bin/bash

echo "🛑 Остановка ZakupBot..."

# Останавливаем контейнеры
docker-compose down

echo "✅ ZakupBot остановлен!"

echo ""
echo "💡 Для полной очистки данных выполните:"
echo "   docker-compose down -v" 