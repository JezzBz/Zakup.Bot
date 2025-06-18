# Запуск ZakupBot в Docker

## Предварительные требования

1. Установленный Docker и Docker Compose
2. Git для клонирования репозитория

## Структура сервисов

Проект состоит из следующих сервисов:
- **telegram-analyzer** (Python) - сервис анализа Telegram каналов
- **zakup-api** (.NET) - основной API сервис
- **minio** - объектное хранилище для файлов
- **db** (PostgreSQL) - база данных

## Запуск

### 1. Клонирование и подготовка

```bash
git clone <repository-url>
cd ZakupBot
```

### 2. Создание необходимых директорий

```bash
mkdir -p tg/sessionsPREM tg/similar
```

### 3. Запуск всех сервисов

```bash
docker-compose up -d
```

### 4. Проверка статуса

```bash
docker-compose ps
```

## Доступ к сервисам

После успешного запуска сервисы будут доступны по следующим адресам:

- **Zakup API**: http://localhost:8080 (HTTP), https://localhost:8081 (HTTPS)
- **Telegram Analyzer**: http://localhost:8000
- **MinIO Console**: http://localhost:9001 (логин: minioadmin, пароль: minioadmin)
- **PostgreSQL**: localhost:5432 (база: PostBot, пользователь: postgres, пароль: the_password)

## Логи и отладка

### Просмотр логов всех сервисов
```bash
docker-compose logs -f
```

### Просмотр логов конкретного сервиса
```bash
docker-compose logs -f zakup-api
docker-compose logs -f telegram-analyzer
docker-compose logs -f minio
docker-compose logs -f db
```

### Остановка сервисов
```bash
docker-compose down
```

### Полная очистка (включая данные)
```bash
docker-compose down -v
```

## Конфигурация

### Переменные окружения

Основные настройки можно изменить через переменные окружения в `docker-compose.yml`:

- **База данных**: POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD
- **MinIO**: MINIO_ROOT_USER, MINIO_ROOT_PASSWORD
- **Telegram Bot**: токен бота в appsettings.json

### Персистентные данные

Данные сохраняются в Docker volumes:
- `postgres_data` - данные PostgreSQL
- `minio_data` - файлы MinIO

## Устранение неполадок

### 1. Проблемы с подключением к базе данных
```bash
docker-compose logs db
```

### 2. Проблемы с MinIO
```bash
docker-compose logs minio
docker-compose logs minio-init
```

### 3. Проблемы с .NET приложением
```bash
docker-compose logs zakup-api
```

### 4. Проблемы с Python приложением
```bash
docker-compose logs telegram-analyzer
```

### 5. Пересборка образов
```bash
docker-compose build --no-cache
docker-compose up -d
```

## Разработка

### Локальная разработка с Docker

Для разработки можно использовать:
```bash
# Запуск только зависимостей
docker-compose up -d db minio

# Запуск приложений локально с подключением к Docker сервисам
```

### Обновление кода

После изменения кода:
```bash
docker-compose build
docker-compose up -d
```

## Безопасность

⚠️ **Важно**: В продакшене обязательно измените:
- Пароли по умолчанию
- Токен Telegram бота
- Настройки безопасности

## Мониторинг

Для мониторинга состояния сервисов используйте:
```bash
docker-compose ps
docker stats
``` 