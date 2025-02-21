from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import httpx
import uvicorn

app = FastAPI(title="Paranormal Activity Hunting Gateway")

# Service URLs
USER_SERVICE_URL = "http://localhost:5170"
SESSION_SERVICE_URL = "http://localhost:5110"

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Create HTTP client
client = httpx.AsyncClient()

async def forward_request(request: Request, service_url: str, path: str) -> JSONResponse:
    try:
        # Construct full URL
        target_url = f"{service_url}/{path}"
        
        # Get request body
        body = await request.body()
        
        # Forward the request
        response = await client.request(
            method=request.method,
            url=target_url,
            headers={
                key: value for key, value in request.headers.items() 
                if key.lower() not in ["host", "content-length"]
            },
            content=body,
            params=request.query_params,
        )
        
        return JSONResponse(
            content=response.json() if response.content else None,
            status_code=response.status_code,
        )
    except httpx.RequestError as e:
        raise HTTPException(
            status_code=503, 
            detail=f"Service unavailable: {str(e)}"
        )

# User Service Routes
@app.post("/user/register")
async def user_register(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register")

@app.post("/user/login")
async def user_login(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login")

@app.get("/user/{id}")
async def get_user(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}")

@app.get("/user/challenges")
async def get_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges")

@app.get("/user/status")
async def user_status(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/status")

# Session Service Routes
@app.post("/session/create")
async def create_session(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create")

@app.get("/session/{id}")
async def get_session(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}")

@app.post("/session/{id}/activate")
async def activate_session(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/activate")

@app.get("/session/existing")
async def get_existing_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing")

@app.get("/session/status")
async def session_status(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/status")

@app.get("/health")
async def health_check():
    """
    Health check endpoint
    """
    return {
        "status": "healthy",
        "services": {
            "user_service": USER_SERVICE_URL,
            "session_service": SESSION_SERVICE_URL
        }
    }

if __name__ == "__main__":
    uvicorn.run(
        "app:app",
        host="0.0.0.0",
        port=8000,
        reload=True
    )