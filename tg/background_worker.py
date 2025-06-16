import asyncio
import logging
import os
from queue_manager import QueueManager, AnalysisStatus
from telegram_manager import analyze_channel_with_all_sessions, SIMILAR_DIR, extract_channel_domain
from notifications import notify_analysis_completion

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class BackgroundWorker:
    def __init__(self, queue_manager: QueueManager):
        self.queue_manager = queue_manager
        self.is_running = False
        self.task = None

    async def start(self):
        """Запускает фоновый процесс обработки очереди"""
        if not self.is_running:
            self.is_running = True
            self.task = asyncio.create_task(self._process_queue())
            logger.info("Background worker started")

    def stop(self):
        """Останавливает фоновый процесс"""
        if self.is_running:
            self.is_running = False
            if self.task:
                self.task.cancel()
            logger.info("Background worker stopped")

    async def _process_queue(self):
        """Обрабатывает очередь анализа"""
        while self.is_running:
            try:
                # Получаем следующий анализ из очереди
                analysis = self.queue_manager.get_next_analysis()
                if not analysis:
                    await asyncio.sleep(1)
                    continue

                guid, channel_name = analysis
                logger.info(f"Processing analysis for GUID: {guid}, channel: {channel_name}")

                try:
                    # Создаем директорию для результатов, если её нет
                    os.makedirs(SIMILAR_DIR, exist_ok=True)

                    # Формируем имя выходного файла
                    channel_domain = extract_channel_domain(channel_name)
                    output_file = os.path.join(SIMILAR_DIR, f"{channel_domain}_similar_channels.xlsx")
                    logger.info(f"Output file will be saved to: {output_file}")

                    try:
                        # Запускаем анализ
                        await analyze_channel_with_all_sessions(channel_name, output_file)
                        
                        # Проверяем, что файл создан
                        if os.path.exists(output_file):
                            logger.info(f"Analysis completed successfully. File saved at: {output_file}")
                            
                            try:
                                # Отправляем уведомление о завершении
                                logger.info(f"Sending completion notification for GUID: {guid}")
                                await notify_analysis_completion(guid, output_file)
                                
                                # Обновляем статус в очереди
                                self.queue_manager.update_status(guid, AnalysisStatus.COMPLETED, output_file)
                                logger.info(f"Status updated to COMPLETED for GUID: {guid}")
                            except Exception as notify_error:
                                error_msg = f"Error sending notification: {str(notify_error)}"
                                logger.error(error_msg)
                                # Даже если уведомление не отправилось, анализ успешен
                                self.queue_manager.update_status(guid, AnalysisStatus.COMPLETED, output_file)
                                logger.info(f"Status updated to COMPLETED for GUID: {guid} despite notification error")
                        else:
                            error_msg = "No similar channels found"
                            logger.warning(f"Analysis completed with warning: {error_msg}")
                            
                            try:
                                # Отправляем уведомление о завершении с ошибкой
                                logger.info(f"Sending completion notification for GUID: {guid} with error")
                                await notify_analysis_completion(guid, None)
                                
                                # Обновляем статус в очереди
                                self.queue_manager.update_status(guid, AnalysisStatus.FAILED, None, error_msg)
                                logger.info(f"Status updated to FAILED for GUID: {guid}")
                            except Exception as notify_error:
                                error_msg = f"Error sending notification: {str(notify_error)}"
                                logger.error(error_msg)
                                self.queue_manager.update_status(guid, AnalysisStatus.FAILED, None, error_msg)
                                logger.info(f"Status updated to FAILED for GUID: {guid} despite notification error")
                    except Exception as analysis_error:
                        error_msg = f"Error during analysis: {str(analysis_error)}"
                        logger.error(error_msg)
                        
                        try:
                            # Отправляем уведомление о завершении с ошибкой
                            logger.info(f"Sending completion notification for GUID: {guid} with error")
                            await notify_analysis_completion(guid, None)
                            
                            # Обновляем статус в очереди
                            self.queue_manager.update_status(guid, AnalysisStatus.FAILED, None, error_msg)
                            logger.info(f"Status updated to FAILED for GUID: {guid}")
                        except Exception as notify_error:
                            error_msg = f"Error sending notification: {str(notify_error)}"
                            logger.error(error_msg)
                            self.queue_manager.update_status(guid, AnalysisStatus.FAILED, None, error_msg)
                            logger.info(f"Status updated to FAILED for GUID: {guid} despite notification error")
                        
                        # Продолжаем работу после ошибки анализа
                        continue

                except Exception as e:
                    error_msg = f"Error during analysis setup: {str(e)}"
                    logger.error(error_msg)
                    
                    try:
                        # Отправляем уведомление о завершении с ошибкой
                        logger.info(f"Sending completion notification for GUID: {guid} with error")
                        await notify_analysis_completion(guid, None)
                        
                        # Обновляем статус в очереди
                        self.queue_manager.update_status(guid, AnalysisStatus.FAILED, None, error_msg)
                        logger.info(f"Status updated to FAILED for GUID: {guid}")
                    except Exception as notify_error:
                        error_msg = f"Error sending notification: {str(notify_error)}"
                        logger.error(error_msg)
                        self.queue_manager.update_status(guid, AnalysisStatus.FAILED, None, error_msg)
                        logger.info(f"Status updated to FAILED for GUID: {guid} despite notification error")
                    
                    # Продолжаем работу после ошибки настройки
                    continue

            except Exception as e:
                logger.error(f"Error in queue processing: {e}")
                # Небольшая пауза перед следующей попыткой
                await asyncio.sleep(1)
                continue 