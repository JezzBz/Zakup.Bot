import aiohttp
import aiofiles
import os
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Константы
CALLBACK_URL = "http://localhost:8000/api/analysis/stream"  # URL вашего API сервера

async def stream_file_to_api(file_path: str, guid: str):
    """Отправляет файл на фиксированный API endpoint"""
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"File not found: {file_path}")

    try:
        async with aiohttp.ClientSession() as session:
            async with aiofiles.open(file_path, 'rb') as file:
                # Читаем файл чанками
                chunk_size = 1024 * 1024  # 1MB chunks
                while True:
                    chunk = await file.read(chunk_size)
                    if not chunk:
                        break
                    
                    # Отправляем чанк на API
                    async with session.post(
                        CALLBACK_URL,
                        data=chunk,
                        headers={
                            'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                            'X-GUID': guid,
                            'Transfer-Encoding': 'chunked'
                        }
                    ) as response:
                        if response.status != 200:
                            error_text = await response.text()
                            raise Exception(f"Failed to send chunk: {response.status}, error: {error_text}")
                        logger.info(f"Successfully sent chunk for GUID: {guid}")
    except Exception as e:
        logger.error(f"Error streaming file: {e}")
        raise 