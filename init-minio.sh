#!/bin/bash

# Ждем пока MinIO запустится
echo "Waiting for MinIO to start..."
until curl -f http://minio:9000/minio/health/live; do
    echo "MinIO is not ready yet..."
    sleep 5
done

echo "MinIO is ready!"

# Создаем bucket если он не существует
mc alias set myminio http://minio:9000 minioadmin minioadmin
mc mb myminio/zakup --ignore-existing

echo "MinIO initialization completed!" 