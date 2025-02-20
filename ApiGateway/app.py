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

async def forward_request(request: Request, service_url: str) -> JSONResponse:
    try:
        # Get the target path
        path = request.url.path
        if path.startswith("/user"):
            path = path[5:]  # Remove /user prefix
        elif path.startswith("/session"):
            path = path[8:]  # Remove /session prefix
            
        # Construct full URL
        target_url = f"{service_url}{path}"
        
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
@app.api_route("/user/{path:path}", methods=["GET", "POST", "PUT", "DELETE"])
async def user_service_proxy(request: Request, path: str):
    """
    Proxy all /user/* requests to the User Management Service
    """
    return await forward_request(request, USER_SERVICE_URL)

# Session Service Routes
@app.api_route("/session/{path:path}", methods=["GET", "POST", "PUT", "DELETE"])
async def session_service_proxy(request: Request, path: str):
    """
    Proxy all /session/* requests to the Session Management Service
    """
    return await forward_request(request, SESSION_SERVICE_URL)

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