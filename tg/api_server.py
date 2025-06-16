from fastapi import FastAPI, HTTPException, Response
from fastapi.responses import StreamingResponse, FileResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
import os
import asyncio
import logging
from queue_manager import QueueManager, AnalysisStatus
from background_worker import BackgroundWorker
from telegram_manager import SIMILAR_DIR

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI()
queue_manager = QueueManager()
background_worker = BackgroundWorker(queue_manager)

# Mount the similar directory for file access
app.mount("/files", StaticFiles(directory=SIMILAR_DIR), name="files")

# Start background worker
@app.on_event("startup")
async def startup_event():
    logger.info("Starting background worker")
    await background_worker.start()

# Stop background worker
@app.on_event("shutdown")
async def shutdown_event():
    logger.info("Stopping background worker")
    background_worker.stop()

class AnalysisRequest(BaseModel):
    guid: str
    channel_name: str

@app.post("/analyze")
async def start_analysis(request: AnalysisRequest):
    logger.info(f"Received analysis request for GUID: {request.guid}, channel: {request.channel_name}")
    
    # Validate input
    if not request.guid or not request.channel_name:
        logger.error("Invalid request: missing GUID or channel name")
        raise HTTPException(status_code=400, detail="GUID and channel name are required")
    
    # Add to queue
    if not queue_manager.add_to_queue(request.guid, request.channel_name):
        logger.warning(f"Analysis with GUID {request.guid} already exists")
        raise HTTPException(status_code=400, detail="Analysis with this GUID already exists")
    
    logger.info(f"Successfully queued analysis for GUID: {request.guid}")
    return {"status": "queued", "guid": request.guid}

@app.get("/analyze/status/{guid}")
async def get_analysis_status(guid: str):
    logger.info(f"Checking status for GUID: {guid}")
    
    if not guid:
        logger.error("Invalid request: missing GUID")
        raise HTTPException(status_code=400, detail="GUID is required")
    
    result = queue_manager.get_analysis_result(guid)
    if not result:
        logger.warning(f"No analysis found for GUID: {guid}")
        raise HTTPException(status_code=404, detail="Analysis not found")
    
    status, result_file, error_message = result
    logger.info(f"Retrieved status for GUID {guid}: {status.value}")
    
    # If analysis is completed, include the file URL
    file_url = None
    if status == AnalysisStatus.COMPLETED and result_file:
        file_name = os.path.basename(result_file)
        file_url = f"/files/{file_name}"
    
    return {
        "status": status.value,
        "result_file": result_file,
        "file_url": file_url,
        "error_message": error_message
    }

@app.get("/files/{file_name}")
async def get_file(file_name: str):
    file_path = os.path.join(SIMILAR_DIR, file_name)
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File not found")
    return FileResponse(file_path)

if __name__ == "__main__":
    import uvicorn
    logger.info("Starting API server")
    uvicorn.run(app, host="0.0.0.0", port=8000) 