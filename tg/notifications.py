import aiohttp
import logging
import os
import json

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

async def notify_analysis_completion(guid: str, file_path: str = None):
    """Отправляет уведомление о завершении анализа на внешний API"""
    try:
        notification_data = {
            "guid": guid,
            "fileUrl": None
        }
        
        if file_path and os.path.exists(file_path):
            file_name = os.path.basename(file_path)
            notification_data["fileUrl"] = f"http://localhost:8000/files/{file_name}"
        
        logger.info(f"Sending notification to API with data: {json.dumps(notification_data, indent=2)}")
        
        async with aiohttp.ClientSession() as session:
            try:
                async with session.post(
                    "http://localhost:5121/api/analysis/complete",
                    json=notification_data,
                    headers={"Content-Type": "application/json"}
                ) as response:
                    response_text = await response.text()
                    if response.status != 200:
                        logger.error(f"Failed to notify analysis completion: {response.status}")
                        logger.error(f"Response text: {response_text}")
                        raise Exception(f"API returned status {response.status}: {response_text}")
                    else:
                        logger.info(f"Successfully notified analysis completion for GUID: {guid}")
                        logger.info(f"API response: {response_text}")
            except aiohttp.ClientError as e:
                logger.error(f"Network error while sending notification: {e}")
                raise
    except Exception as e:
        logger.error(f"Error notifying analysis completion: {e}")
        raise 