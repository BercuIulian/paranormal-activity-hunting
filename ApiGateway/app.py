from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import httpx
import uvicorn
import json
from fastapi_cache import FastAPICache
from fastapi_cache.backends.redis import RedisBackend
from fastapi_cache.decorator import cache
import redis
import os
from datetime import timedelta

app = FastAPI(title="Paranormal Activity Hunting Gateway")

# Service URLs
USER_SERVICE_URL = "http://user-management-api:8080"
SESSION_SERVICE_URL = "http://session-management-api:8080"

# Redis configuration
REDIS_HOST = os.getenv("REDIS_HOST", "redis")
REDIS_PORT = int(os.getenv("REDIS_PORT", 6379))
REDIS_PASSWORD = os.getenv("REDIS_PASSWORD", "")
REDIS_DB = int(os.getenv("REDIS_DB", 0))

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
        
        # Get headers
        headers = {
            key: value for key, value in request.headers.items() 
            if key.lower() not in ["host", "content-length"]
        }
        headers["Content-Type"] = "application/json"
        
        # Forward the request
        async with httpx.AsyncClient() as client:
            response = await client.request(
                method=request.method,
                url=target_url,
                headers=headers,
                content=body,
                params=request.query_params,
            )
            
            # Try to parse JSON response
            try:
                response_data = response.json() if response.content else None
            except json.JSONDecodeError:
                response_data = {"message": response.text}
            
            return JSONResponse(
                content=response_data,
                status_code=response.status_code
            )
            
    except httpx.RequestError as e:
        return JSONResponse(
            content={"error": f"Service unavailable: {str(e)}"},
            status_code=503
        )
    except Exception as e:
        return JSONResponse(
            content={"error": f"Internal server error: {str(e)}"},
            status_code=500
        )

# Initialize Redis cache on startup
@app.on_event("startup")
async def startup():
    redis_client = redis.Redis(
        host=os.getenv("REDIS_HOST", "redis"),  # Use environment variable with default
        port=int(os.getenv("REDIS_PORT", "6379")),
        db=0,
        decode_responses=True
    )
    FastAPICache.init(RedisBackend(redis_client), prefix="fastapi-cache")

# User Service Routes
# Registration Related Endpoints    
@app.post("/user/register")
async def user_register(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register")

@app.post("/user/register/quick")
async def user_register_quick(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/quick")

@app.post("/user/register/validate-email")
async def user_validate_email(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/validate-email")

@app.get("/user/register/check-username")
async def user_check_username(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/check-username")

@app.post("/user/register/send-confirmation")
async def user_send_confirmation(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/send-confirmation")

@app.post("/user/register/resend-confirmation")
async def user_resend_confirmation(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/resend-confirmation")

@app.post("/user/register/social")
async def user_register_social(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/social")

@app.post("/user/register/phone")
async def user_register_phone(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/phone")

@app.post("/user/register/admin")
async def user_register_admin(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/register/admin")

# Login Related Endpoints
@app.post("/user/login")
async def user_login(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login")

@app.post("/user/login/quick")
async def user_login_quick(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/quick")

@app.post("/user/login/social")
async def user_login_social(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/social")

@app.post("/user/login/admin")
async def user_login_admin(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/admin")

@app.post("/user/login/phone")
async def user_login_phone(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/phone")

@app.get("/user/login/attempts")
async def user_login_attempts(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/attempts")

@app.post("/user/login/reset-password")
async def user_reset_password(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/reset-password")

@app.post("/user/login/email")
async def user_login_email(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/email")

@app.post("/user/login/secure-questions")
async def user_login_secure_questions(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/login/secure-questions")

# User Profile Related Endpoints - ADDING CACHE HERE
@app.get("/user/{id}")
@cache(expire=300)  # Cache for 5 minutes
async def get_user(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}")

@app.get("/user/{id}/profile")
@cache(expire=300)  # Cache for 5 minutes
async def get_user_profile(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/profile")

@app.get("/user/{id}/xp")
@cache(expire=60)  # Cache for 1 minute
async def get_user_xp(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/xp")

@app.get("/user/{id}/challenges-completed")
@cache(expire=300)  # Cache for 5 minutes
async def get_user_challenges_completed(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/challenges-completed")

@app.get("/user/{id}/inventory")
@cache(expire=300)  # Cache for 5 minutes
async def get_user_inventory(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/inventory")

@app.get("/user/{id}/created")
@cache(expire=3600)  # Cache for 1 hour
async def get_user_created(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/created")

@app.get("/user/{id}/updated")
@cache(expire=300)  # Cache for 5 minutes
async def get_user_updated(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/updated")

@app.get("/user/{id}/admin-status")
@cache(expire=600)  # Cache for 10 minutes
async def get_user_admin_status(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/admin-status")

@app.put("/user/{id}/update")
async def update_user_profile(request: Request, id: str):
    # No cache for update operations
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/update")

# Challenge Related Endpoints
@app.get("/user/challenges")
@cache(expire=600)  # Cache for 10 minutes
async def get_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges")

@app.post("/user/challenges/add")
async def add_challenge(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/add")

@app.post("/user/challenges/start")
async def start_challenge(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/start")

@app.post("/user/challenges/assign")
async def assign_challenge(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/assign")

@app.post("/user/challenges/complete")
async def complete_challenge(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/complete")

@app.get("/user/challenges/completed")
@cache(expire=300)  # Cache for 5 minutes
async def get_completed_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/completed")

@app.get("/user/challenges/daily")
@cache(expire=3600)  # Cache for 1 hour
async def get_daily_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/daily")

@app.get("/user/challenges/weekly")
@cache(expire=3600 * 6)  # Cache for 6 hours
async def get_weekly_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/weekly")

@app.get("/user/challenges/rewards")
@cache(expire=3600)  # Cache for 1 hour
async def get_challenge_rewards(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/rewards")

#Health Check
@app.get("/user/status")
async def user_status(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/status")

# Session Service Routes
# Session Creation Endpoints
# @app.post("/session/create")
# async def create_session(request: Request):
#     return await forward_request(request, SESSION_SERVICE_URL, "session/create")

@app.post("/session/create/quick")
async def create_quick_session(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create/quick")

@app.post("/session/create/private")
async def create_private_session(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create/private")

@app.post("/session/create/test")
async def create_test_session(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create/test")

@app.post("/session/create/group")
async def create_group_session(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create/group")

@app.post("/session/create/schedule")
async def create_scheduled_session(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create/schedule")

@app.post("/session/create/set-challenges")
async def create_session_with_challenges(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create/set-challenges")

@app.post("/session/create/set-rules")
async def create_session_with_rules(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create/set-rules")

# Session Details Endpoints - ADDING CACHE HERE
@app.get("/session/{id}")
@cache(expire=300)  # Cache for 5 minutes
async def get_session(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}")

@app.get("/session/{id}/details")
@cache(expire=300)  # Cache for 5 minutes
async def get_session_details(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/details")

@app.get("/session/{id}/participants")
@cache(expire=60)  # Cache for 1 minute
async def get_session_participants(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/participants")

@app.get("/session/{id}/logs")
@cache(expire=120)  # Cache for 2 minutes
async def get_session_logs(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/logs")

@app.get("/session/{id}/challenges")
@cache(expire=300)  # Cache for 5 minutes
async def get_session_challenges(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/challenges")

@app.get("/session/{id}/location")
@cache(expire=300)  # Cache for 5 minutes
async def get_session_location(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/location")

@app.get("/session/{id}/owner")
@cache(expire=600)  # Cache for 10 minutes
async def get_session_owner(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/owner")

@app.get("/session/{id}/rules")
@cache(expire=600)  # Cache for 10 minutes
async def get_session_rules(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/rules")

@app.get("/session/{id}/created")
@cache(expire=3600)  # Cache for 1 hour
async def get_session_created_date(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/created")

# Session Activation Endpoints
@app.post("/session/{id}/activate")
async def activate_session(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/activate")

@app.post("/session/activate/user/{id}")
async def activate_user_session(id: str, request: Request):
    try:
        body = await request.json()
        return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/user/{id}")
    except json.JSONDecodeError:
        return JSONResponse(
            content={"error": "Invalid JSON in request body"},
            status_code=400
        )

@app.post("/session/activate/challenge/{id}")
async def activate_challenge(id: str, request: Request):
    try:
        body = await request.json()
        return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/challenge/{id}")
    except json.JSONDecodeError:
        return JSONResponse(
            content={"error": "Invalid JSON in request body"},
            status_code=400
        )

@app.post("/session/activate/recent/{id}")
async def activate_session_recent(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/recent/{id}")

@app.get("/session/activate/rule/{id}")
async def activate_session_rule(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/rule/{id}")

@app.post("/session/activate/time/{id}")
async def activate_session_time(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/time/{id}")

# Session Listing Endpoints - ADDING CACHE HERE
@app.get("/session/existing")
@cache(expire=120)  # Cache for 2 minutes
async def get_existing_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing")

@app.get("/session/existing/open")
@cache(expire=60)  # Cache for 1 minute
async def get_open_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/open")

@app.get("/session/existing/nearby")
@cache(expire=60)  # Cache for 1 minute
async def get_nearby_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/nearby")

@app.get("/session/existing/private")
@cache(expire=300)  # Cache for 5 minutes
async def get_private_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/private")

@app.get("/session/existing/completed")
@cache(expire=600)  # Cache for 10 minutes
async def get_completed_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/completed")

@app.get("/session/existing/popular")
@cache(expire=300)  # Cache for 5 minutes
async def get_popular_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/popular")

@app.get("/session/existing/recently-updated")
@cache(expire=60)  # Cache for 1 minute
async def get_recently_updated_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/recently-updated")

@app.get("/session/existing/joinable")
@cache(expire=60)  # Cache for 1 minute
async def get_joinable_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/joinable")

@app.get("/session/existing/category/{type}")
@cache(expire=300)  # Cache for 5 minutes
async def get_sessions_by_category(request: Request, type: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/existing/category/{type}")

#Health Check
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

# Cache management endpoints
@app.post("/cache/clear")
async def clear_cache():
    """
    Clear the entire cache
    """
    await FastAPICache.clear()
    return {"message": "Cache cleared successfully"}

@app.post("/cache/clear/{key}")
async def clear_cache_key(key: str):
    """
    Clear a specific cache key
    """
    redis_client = redis.Redis(
        host=REDIS_HOST,
        port=REDIS_PORT,
        password=REDIS_PASSWORD,
        db=REDIS_DB
    )
    redis_key = f"fastapi-cache:{key}"
    deleted = redis_client.delete(redis_key)
    redis_client.close()
    
    if deleted:
        return {"message": f"Cache key '{key}' cleared successfully"}
    else:
        return {"message": f"Cache key '{key}' not found"}
    
async def invalidate_cache_for_user(user_id: str):
    """
    Invalidate cache for a specific user
    """
    redis_client = redis.Redis(
        host="localhost",
        port=6379,
        db=0
    )
    
    # Get all keys that might be related to this user
    keys = redis_client.keys(f"fastapi-cache:*user*{user_id}*")
    
    # Delete all matching keys
    if keys:
        redis_client.delete(*keys)
    
    redis_client.close()

if __name__ == "__main__":
    uvicorn.run(
        "app:app",
        host="0.0.0.0",
        port=8000,
        reload=True
    )