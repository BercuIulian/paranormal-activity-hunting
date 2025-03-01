from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse, Response
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
import random
from typing import List
import time
# Import Prometheus client
from prometheus_client import Counter, Histogram, Gauge, Summary, generate_latest, CONTENT_TYPE_LATEST
from functools import wraps

app = FastAPI(title="Paranormal Activity Hunting Gateway")

# Service URLs - Now we'll use lists of URLs for load balancing
USER_SERVICE_URLS = os.getenv("USER_SERVICE_URLS", "http://user-management-api-1:8080,http://user-management-api-2:8080,http://user-management-api-3:8080").split(",")
SESSION_SERVICE_URLS = os.getenv("SESSION_SERVICE_URLS", "http://session-management-api-1:8080,http://session-management-api-2:8080,http://session-management-api-3:8080").split(",")

# Counters for Round Robin load balancing
user_service_counter = 0
session_service_counter = 0

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

# Define Prometheus metrics
REQUESTS = Counter(
    'api_gateway_requests_total', 
    'Total count of requests by method and endpoint',
    ['method', 'endpoint', 'service']
)

RESPONSES = Counter(
    'api_gateway_responses_total', 
    'Total count of responses by method, endpoint and status',
    ['method', 'endpoint', 'status', 'service']
)

LATENCY = Histogram(
    'api_gateway_request_latency_seconds', 
    'Request latency in seconds',
    ['method', 'endpoint', 'service']
)

ACTIVE_REQUESTS = Gauge(
    'api_gateway_active_requests', 
    'Number of active requests',
    ['method', 'service']
)

CACHE_HITS = Counter(
    'api_gateway_cache_hits_total',
    'Total count of cache hits',
    ['endpoint']
)

CACHE_MISSES = Counter(
    'api_gateway_cache_misses_total',
    'Total count of cache misses',
    ['endpoint']
)

SERVICE_AVAILABILITY = Gauge(
    'api_gateway_service_availability',
    'Service availability status (1=up, 0=down)',
    ['service_name', 'instance']
)

# Function to get the next service URL using Round Robin
def get_next_user_service_url() -> str:
    global user_service_counter
    url = USER_SERVICE_URLS[user_service_counter % len(USER_SERVICE_URLS)]
    user_service_counter += 1
    return url

def get_next_session_service_url() -> str:
    global session_service_counter
    url = SESSION_SERVICE_URLS[session_service_counter % len(SESSION_SERVICE_URLS)]
    session_service_counter += 1
    return url

async def forward_request(request: Request, service_type: str, path: str) -> JSONResponse:
    try:
        # Get the next service URL using Round Robin
        if service_type == "user":
            service_url = get_next_user_service_url()
        else:  # session
            service_url = get_next_session_service_url()
        
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
        
        # Increment request counter
        REQUESTS.labels(method=request.method, endpoint=path, service=service_type).inc()
        
        # Track active requests
        ACTIVE_REQUESTS.labels(method=request.method, service=service_type).inc()
        
        # Track request latency
        start_time = time.time()
        
        try:
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
                
                # Record latency
                LATENCY.labels(method=request.method, endpoint=path, service=service_type).observe(time.time() - start_time)
                
                # Record response
                RESPONSES.labels(
                    method=request.method, 
                    endpoint=path, 
                    status=response.status_code,
                    service=service_type
                ).inc()
                
                # Decrement active requests
                ACTIVE_REQUESTS.labels(method=request.method, service=service_type).dec()
                
                return JSONResponse(
                    content=response_data,
                    status_code=response.status_code
                )
        except Exception as e:
            # Decrement active requests in case of error
            ACTIVE_REQUESTS.labels(method=request.method, service=service_type).dec()
            raise e
            
    except httpx.RequestError as e:
        # If a service instance is down, try the next one
        if service_type == "user" and len(USER_SERVICE_URLS) > 1:
            # Try another user service instance
            return await forward_request(request, service_type, path)
        elif service_type == "session" and len(SESSION_SERVICE_URLS) > 1:
            # Try another session service instance
            return await forward_request(request, service_type, path)
        else:
            # Decrement active requests in case of error
            ACTIVE_REQUESTS.labels(method=request.method, service=service_type).dec()
            
            # Record response
            RESPONSES.labels(
                method=request.method, 
                endpoint=path, 
                status=503,
                service=service_type
            ).inc()
            
            return JSONResponse(
                content={"error": f"Service unavailable: {str(e)}"},
                status_code=503
            )
    except Exception as e:
        # Decrement active requests in case of error
        ACTIVE_REQUESTS.labels(method=request.method, service=service_type).dec()
        
        # Record response
        RESPONSES.labels(
            method=request.method, 
            endpoint=path, 
            status=500,
            service=service_type
        ).inc()
        
        return JSONResponse(
            content={"error": f"Internal server error: {str(e)}"},
            status_code=500
        )

from functools import wraps

def tracked_cache(expire=300):
    def decorator(func):
        @wraps(func)
        async def wrapper(*args, **kwargs):
            # Extract request object
            request = next((arg for arg in args if isinstance(arg, Request)), None)
            if request is None:
                # If no request object, just call the original function
                return await func(*args, **kwargs)

            # Construct cache key using path and query parameters
            path_params = request.path_params
            query_params = request.query_params
            cache_key = f"{func.__name__}:{str(path_params)}:{str(query_params)}"

            # Check if result is in cache
            redis_client = redis.Redis(
                host=REDIS_HOST,
                port=REDIS_PORT,
                password=REDIS_PASSWORD,
                db=REDIS_DB
            )
            
            cached_result = redis_client.get(f"fastapi-cache:{cache_key}")
            if cached_result:
                CACHE_HITS.labels(endpoint=func.__name__).inc()
                redis_client.close()
                return JSONResponse(content=json.loads(cached_result))
            
            # If not in cache, call the original function
            CACHE_MISSES.labels(endpoint=func.__name__).inc()
            result = await func(*args, **kwargs)

            # Cache the result
            redis_client.setex(
                f"fastapi-cache:{cache_key}",
                expire,
                json.dumps(result.body.decode())
            )
            redis_client.close()

            return result
        
        return wrapper
    return decorator

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
    
    # Initialize service availability metrics
    for i, url in enumerate(USER_SERVICE_URLS):
        SERVICE_AVAILABILITY.labels(service_name="user-management", instance=f"user-management-api-{i+1}").set(1)
    
    for i, url in enumerate(SESSION_SERVICE_URLS):
        SERVICE_AVAILABILITY.labels(service_name="session-management", instance=f"session-management-api-{i+1}").set(1)

# Expose Prometheus metrics endpoint
@app.get("/metrics")
async def metrics():
    return Response(generate_latest(), media_type=CONTENT_TYPE_LATEST)

# User Service Routes
# Registration Related Endpoints    
@app.post("/user/register")
async def user_register(request: Request):
    return await forward_request(request, "user", "user/register")

@app.post("/user/register/quick")
async def user_register_quick(request: Request):
    return await forward_request(request, "user", "user/register/quick")

@app.post("/user/register/validate-email")
async def user_validate_email(request: Request):
    return await forward_request(request, "user", "user/register/validate-email")

@app.get("/user/register/check-username")
async def user_check_username(request: Request):
    return await forward_request(request, "user", "user/register/check-username")

@app.post("/user/register/send-confirmation")
async def user_send_confirmation(request: Request):
    return await forward_request(request, "user", "user/register/send-confirmation")

@app.post("/user/register/resend-confirmation")
async def user_resend_confirmation(request: Request):
    return await forward_request(request, "user", "user/register/resend-confirmation")

@app.post("/user/register/social")
async def user_register_social(request: Request):
    return await forward_request(request, "user", "user/register/social")

@app.post("/user/register/phone")
async def user_register_phone(request: Request):
    return await forward_request(request, "user", "user/register/phone")

@app.post("/user/register/admin")
async def user_register_admin(request: Request):
    return await forward_request(request, "user", "user/register/admin")

# Login Related Endpoints
@app.post("/user/login")
async def user_login(request: Request):
    return await forward_request(request, "user", "user/login")

@app.post("/user/login/quick")
async def user_login_quick(request: Request):
    return await forward_request(request, "user", "user/login/quick")

@app.post("/user/login/social")
async def user_login_social(request: Request):
    return await forward_request(request, "user", "user/login/social")

@app.post("/user/login/admin")
async def user_login_admin(request: Request):
    return await forward_request(request, "user", "user/login/admin")

@app.post("/user/login/phone")
async def user_login_phone(request: Request):
    return await forward_request(request, "user", "user/login/phone")

@app.get("/user/login/attempts")
async def user_login_attempts(request: Request):
    return await forward_request(request, "user", "user/login/attempts")

@app.post("/user/login/reset-password")
async def user_reset_password(request: Request):
    return await forward_request(request, "user", "user/login/reset-password")

@app.post("/user/login/email")
async def user_login_email(request: Request):
    return await forward_request(request, "user", "user/login/email")

@app.post("/user/login/secure-questions")
async def user_login_secure_questions(request: Request):
    return await forward_request(request, "user", "user/login/secure-questions")

# User Profile Related Endpoints - ADDING CACHE HERE
@app.get("/user/{id}")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_user(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}")

@app.get("/user/{id}/profile")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_user_profile(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}/profile")

@app.get("/user/{id}/xp")
@tracked_cache(expire=60)  # Cache for 1 minute
async def get_user_xp(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}/xp")

@app.get("/user/{id}/challenges-completed")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_user_challenges_completed(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}/challenges-completed")

@app.get("/user/{id}/inventory")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_user_inventory(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}/inventory")

@app.get("/user/{id}/created")
@tracked_cache(expire=3600)  # Cache for 1 hour
async def get_user_created(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}/created")

@app.get("/user/{id}/updated")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_user_updated(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}/updated")

@app.get("/user/{id}/admin-status")
@tracked_cache(expire=600)  # Cache for 10 minutes
async def get_user_admin_status(request: Request, id: str):
    return await forward_request(request, "user", f"user/{id}/admin-status")

@app.put("/user/{id}/update")
async def update_user_profile(request: Request, id: str):
    # No cache for update operations
    return await forward_request(request, "user", f"user/{id}/update")

# Challenge Related Endpoints
@app.get("/user/challenges")
@tracked_cache(expire=600)  # Cache for 10 minutes
async def get_challenges(request: Request):
    return await forward_request(request, "user", "user/challenges")

@app.post("/user/challenges/add")
async def add_challenge(request: Request):
    return await forward_request(request, "user", "user/challenges/add")

@app.post("/user/challenges/start")
async def start_challenge(request: Request):
    return await forward_request(request, "user", "user/challenges/start")

@app.post("/user/challenges/assign")
async def assign_challenge(request: Request):
    return await forward_request(request, "user", "user/challenges/assign")

@app.post("/user/challenges/complete")
async def complete_challenge(request: Request):
    return await forward_request(request, "user", "user/challenges/complete")

@app.get("/user/challenges/completed")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_completed_challenges(request: Request):
    return await forward_request(request, "user", "user/challenges/completed")

@app.get("/user/challenges/daily")
@tracked_cache(expire=3600)  # Cache for 1 hour
async def get_daily_challenges(request: Request):
    return await forward_request(request, "user", "user/challenges/daily")

@app.get("/user/challenges/weekly")
@tracked_cache(expire=3600 * 6)  # Cache for 6 hours
async def get_weekly_challenges(request: Request):
    return await forward_request(request, "user", "user/challenges/weekly")

@app.get("/user/challenges/rewards")
@tracked_cache(expire=3600)  # Cache for 1 hour
async def get_challenge_rewards(request: Request):
    return await forward_request(request, "user", "user/challenges/rewards")

#Health Check
@app.get("/user/status")
async def user_status(request: Request):
    return await forward_request(request, "user", "user/status")

# Session Service Routes
# Session Creation Endpoints
@app.post("/session/create/quick")
async def create_quick_session(request: Request):
    return await forward_request(request, "session", "session/create/quick")

@app.post("/session/create/private")
async def create_private_session(request: Request):
    return await forward_request(request, "session", "session/create/private")

@app.post("/session/create/test")
async def create_test_session(request: Request):
    return await forward_request(request, "session", "session/create/test")

@app.post("/session/create/group")
async def create_group_session(request: Request):
    return await forward_request(request, "session", "session/create/group")

@app.post("/session/create/schedule")
async def create_scheduled_session(request: Request):
    return await forward_request(request, "session", "session/create/schedule")

@app.post("/session/create/set-challenges")
async def create_session_with_challenges(request: Request):
    return await forward_request(request, "session", "session/create/set-challenges")

@app.post("/session/create/set-rules")
async def create_session_with_rules(request: Request):
    return await forward_request(request, "session", "session/create/set-rules")

# Session Details Endpoints - ADDING CACHE HERE
@app.get("/session/{id}")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_session(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}")

@app.get("/session/{id}/details")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_session_details(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/details")

@app.get("/session/{id}/participants")
@tracked_cache(expire=60)  # Cache for 1 minute
async def get_session_participants(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/participants")

@app.get("/session/{id}/logs")
@tracked_cache(expire=120)  # Cache for 2 minutes
async def get_session_logs(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/logs")

@app.get("/session/{id}/challenges")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_session_challenges(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/challenges")

@app.get("/session/{id}/location")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_session_location(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/location")

@app.get("/session/{id}/owner")
@tracked_cache(expire=600)  # Cache for 10 minutes
async def get_session_owner(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/owner")

@app.get("/session/{id}/rules")
@tracked_cache(expire=600)  # Cache for 10 minutes
async def get_session_rules(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/rules")

@app.get("/session/{id}/created")
@tracked_cache(expire=3600)  # Cache for 1 hour
async def get_session_created_date(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/created")

# Session Activation Endpoints
@app.post("/session/{id}/activate")
async def activate_session(request: Request, id: str):
    return await forward_request(request, "session", f"session/{id}/activate")

@app.post("/session/activate/user/{id}")
async def activate_user_session(id: str, request: Request):
    try:
        body = await request.json()
        return await forward_request(request, "session", f"session/activate/user/{id}")
    except json.JSONDecodeError:
        return JSONResponse(
            content={"error": "Invalid JSON in request body"},
            status_code=400
        )

@app.post("/session/activate/challenge/{id}")
async def activate_challenge(id: str, request: Request):
    try:
        body = await request.json()
        return await forward_request(request, "session", f"session/activate/challenge/{id}")
    except json.JSONDecodeError:
        return JSONResponse(
            content={"error": "Invalid JSON in request body"},
            status_code=400
        )

@app.post("/session/activate/recent/{id}")
async def activate_session_recent(request: Request, id: str):
    return await forward_request(request, "session", f"session/activate/recent/{id}")

@app.get("/session/activate/rule/{id}")
async def activate_session_rule(request: Request, id: str):
    return await forward_request(request, "session", f"session/activate/rule/{id}")

@app.post("/session/activate/time/{id}")
async def activate_session_time(request: Request, id: str):
    return await forward_request(request, "session", f"session/activate/time/{id}")

# Session Listing Endpoints - ADDING CACHE HERE
@app.get("/session/existing")
@tracked_cache(expire=120)  # Cache for 2 minutes
async def get_existing_sessions(request: Request):
    return await forward_request(request, "session", "session/existing")

@app.get("/session/existing/open")
@tracked_cache(expire=60)  # Cache for 1 minute
async def get_open_sessions(request: Request):
    return await forward_request(request, "session", "session/existing/open")

@app.get("/session/existing/nearby")
@tracked_cache(expire=60)  # Cache for 1 minute
async def get_nearby_sessions(request: Request):
    return await forward_request(request, "session", "session/existing/nearby")

@app.get("/session/existing/private")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_private_sessions(request: Request):
    return await forward_request(request, "session", "session/existing/private")

@app.get("/session/existing/completed")
@tracked_cache(expire=600)  # Cache for 10 minutes
async def get_completed_sessions(request: Request):
    return await forward_request(request, "session", "session/existing/completed")

@app.get("/session/existing/popular")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_popular_sessions(request: Request):
    return await forward_request(request, "session", "session/existing/popular")

@app.get("/session/existing/recently-updated")
@tracked_cache(expire=60)  # Cache for 1 minute
async def get_recently_updated_sessions(request: Request):
    return await forward_request(request, "session", "session/existing/recently-updated")

@app.get("/session/existing/joinable")
@tracked_cache(expire=60)  # Cache for 1 minute
async def get_joinable_sessions(request: Request):
    return await forward_request(request, "session", "session/existing/joinable")

@app.get("/session/existing/category/{type}")
@tracked_cache(expire=300)  # Cache for 5 minutes
async def get_sessions_by_category(request: Request, type: str):
    return await forward_request(request, "session", f"session/existing/category/{type}")

#Health Check
@app.get("/session/status")
async def session_status(request: Request):
    return await forward_request(request, "session", "session/status")

@app.get("/health")
async def health_check():
    """
    Health check endpoint that also shows the status of all service instances
    """
    user_services_status = {}
    session_services_status = {}
    
    # Check user services
    for i, url in enumerate(USER_SERVICE_URLS):
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(f"{url}/health", timeout=2.0)
                is_healthy = response.status_code == 200
                user_services_status[f"user-management-api-{i+1}"] = "healthy" if is_healthy else "unhealthy"
                # Update service availability metric
                SERVICE_AVAILABILITY.labels(
                    service_name="user-management", 
                    instance=f"user-management-api-{i+1}"
                ).set(1 if is_healthy else 0)
        except Exception:
            user_services_status[f"user-management-api-{i+1}"] = "unreachable"
            # Update service availability metric
            SERVICE_AVAILABILITY.labels(
                service_name="user-management", 
                instance=f"user-management-api-{i+1}"
            ).set(0)
    
    # Check session services
    for i, url in enumerate(SESSION_SERVICE_URLS):
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(f"{url}/health", timeout=2.0)
                is_healthy = response.status_code == 200
                session_services_status[f"session-management-api-{i+1}"] = "healthy" if is_healthy else "unhealthy"
                # Update service availability metric
                SERVICE_AVAILABILITY.labels(
                    service_name="session-management", 
                    instance=f"session-management-api-{i+1}"
                ).set(1 if is_healthy else 0)
        except Exception:
            session_services_status[f"session-management-api-{i+1}"] = "unreachable"
            # Update service availability metric
            SERVICE_AVAILABILITY.labels(
                service_name="session-management", 
                instance=f"session-management-api-{i+1}"
            ).set(0)
    
    return {
        "status": "healthy",
        "load_balancing": {
            "algorithm": "round_robin",
            "user_services": user_services_status,
            "session_services": session_services_status
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
        host=REDIS_HOST,
        port=REDIS_PORT,
        password=REDIS_PASSWORD,
        db=REDIS_DB
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

