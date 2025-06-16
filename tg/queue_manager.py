from enum import Enum
import sqlite3
import os
import logging
from typing import Optional, Tuple, List
from datetime import datetime, timedelta

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class AnalysisStatus(Enum):
    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"

class QueueManager:
    def __init__(self, db_path: str = "analysis_queue.db"):
        self.db_path = db_path
        logger.info(f"Initializing QueueManager with database at {db_path}")
        self._init_db()
        self.cleanup_old_results()

    def _init_db(self):
        """Initialize SQLite database with required tables"""
        try:
            with sqlite3.connect(self.db_path) as conn:
                conn.execute("""
                    CREATE TABLE IF NOT EXISTS analysis_queue (
                        guid TEXT PRIMARY KEY,
                        channel_name TEXT NOT NULL,
                        status TEXT NOT NULL,
                        result_file TEXT,
                        error_message TEXT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )
                """)
                logger.info("Database initialized successfully")
        except Exception as e:
            logger.error(f"Error initializing database: {e}")
            raise

    def guid_exists(self, guid: str) -> bool:
        """Check if GUID exists in the queue"""
        try:
            with sqlite3.connect(self.db_path) as conn:
                cursor = conn.execute(
                    "SELECT guid, status FROM analysis_queue WHERE guid = ?",
                    (guid,)
                )
                result = cursor.fetchone()
                return result is not None
        except Exception as e:
            logger.error(f"Error checking GUID {guid}: {e}")
            return False

    def add_to_queue(self, guid: str, channel_name: str) -> bool:
        """Add new analysis to queue. Returns True if added successfully, False if GUID already exists."""
        try:
            # First, let's check what's in the database
            with sqlite3.connect(self.db_path) as conn:
                cursor = conn.execute("SELECT guid, status FROM analysis_queue")
                all_records = cursor.fetchall()
                logger.info(f"Current records in database: {all_records}")

            if self.guid_exists(guid):
                logger.warning(f"GUID {guid} already exists in queue")
                return False

            with sqlite3.connect(self.db_path) as conn:
                conn.execute(
                    "INSERT INTO analysis_queue (guid, channel_name, status, created_at) VALUES (?, ?, ?, datetime('now'))",
                    (guid, channel_name, AnalysisStatus.PENDING.value)
                )
                logger.info(f"Successfully added analysis for GUID {guid} and channel {channel_name}")
                return True
        except sqlite3.IntegrityError as e:
            logger.error(f"Integrity error adding GUID {guid}: {e}")
            return False
        except Exception as e:
            logger.error(f"Error adding to queue: {e}")
            return False

    def get_next_analysis(self) -> Optional[Tuple[str, str]]:
        """Get next pending analysis from queue"""
        try:
            with sqlite3.connect(self.db_path) as conn:
                cursor = conn.execute(
                    "SELECT guid, channel_name FROM analysis_queue WHERE status = ? ORDER BY created_at ASC LIMIT 1",
                    (AnalysisStatus.PENDING.value,)
                )
                result = cursor.fetchone()
                if result:
                    logger.info(f"Retrieved next analysis: GUID {result[0]}, channel {result[1]}")
                return result if result else None
        except Exception as e:
            logger.error(f"Error getting next analysis: {e}")
            return None

    def update_status(self, guid: str, status: AnalysisStatus, result_file: Optional[str] = None, error_message: Optional[str] = None):
        """Update analysis status and optional result file or error message"""
        try:
            with sqlite3.connect(self.db_path) as conn:
                conn.execute(
                    "UPDATE analysis_queue SET status = ?, result_file = ?, error_message = ? WHERE guid = ?",
                    (status.value, result_file, error_message, guid)
                )
                logger.info(f"Updated status for GUID {guid} to {status.value}")
        except Exception as e:
            logger.error(f"Error updating status for GUID {guid}: {e}")

    def get_analysis_result(self, guid: str) -> Optional[Tuple[AnalysisStatus, Optional[str], Optional[str]]]:
        """Get analysis status and result file path if available"""
        try:
            with sqlite3.connect(self.db_path) as conn:
                cursor = conn.execute(
                    "SELECT status, result_file, error_message FROM analysis_queue WHERE guid = ?",
                    (guid,)
                )
                result = cursor.fetchone()
                if result:
                    status, result_file, error_message = result
                    logger.info(f"Retrieved result for GUID {guid}: status={status}")
                    return AnalysisStatus(status), result_file, error_message
                logger.info(f"No result found for GUID {guid}")
                return None
        except Exception as e:
            logger.error(f"Error getting result for GUID {guid}: {e}")
            return None

    def cleanup_old_results(self, days: int = 1):
        """Clean up old completed and failed analyses"""
        try:
            with sqlite3.connect(self.db_path) as conn:
                # Delete old records
                conn.execute(
                    "DELETE FROM analysis_queue WHERE status IN (?, ?) AND created_at < datetime('now', ?)",
                    (AnalysisStatus.COMPLETED.value, AnalysisStatus.FAILED.value, f'-{days} days')
                )
                # Also clean up any stuck processing records older than 1 hour
                conn.execute(
                    "DELETE FROM analysis_queue WHERE status = ? AND created_at < datetime('now', '-1 hour')",
                    (AnalysisStatus.PROCESSING.value,)
                )
                logger.info("Cleaned up old records from database")
        except Exception as e:
            logger.error(f"Error cleaning up old records: {e}") 