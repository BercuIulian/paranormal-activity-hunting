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

# User Profile Related Endpoints
@app.get("/user/{id}")
async def get_user(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}")

@app.get("/user/{id}/profile")
async def get_user_profile(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/profile")

@app.get("/user/{id}/xp")
async def get_user_xp(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/xp")

@app.get("/user/{id}/challenges-completed")
async def get_user_challenges_completed(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/challenges-completed")

@app.get("/user/{id}/inventory")
async def get_user_inventory(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/inventory")

@app.get("/user/{id}/created")
async def get_user_created(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/created")

@app.get("/user/{id}/updated")
async def get_user_updated(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/updated")

@app.get("/user/{id}/admin-status")
async def get_user_admin_status(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/admin-status")

@app.put("/user/{id}/update")
async def update_user_profile(request: Request, id: str):
    return await forward_request(request, USER_SERVICE_URL, f"user/{id}/update")

# Challenge Related Endpoints
@app.get("/user/challenges")
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
async def get_completed_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/completed")

@app.get("/user/challenges/daily")
async def get_daily_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/daily")

@app.get("/user/challenges/weekly")
async def get_weekly_challenges(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/weekly")

@app.get("/user/challenges/rewards")
async def get_challenge_rewards(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/challenges/rewards")

#Health Check
@app.get("/user/status")
async def user_status(request: Request):
    return await forward_request(request, USER_SERVICE_URL, "user/status")

# Session Service Routes
# Session Creation Endpoints
@app.post("/session/create")
async def create_session(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/create")

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

# Session Details Endpoints
@app.get("/session/{id}")
async def get_session(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}")

@app.get("/session/{id}/details")
async def get_session_details(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/details")

@app.get("/session/{id}/participants")
async def get_session_participants(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/participants")

@app.get("/session/{id}/logs")
async def get_session_logs(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/logs")

@app.get("/session/{id}/challenges")
async def get_session_challenges(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/challenges")

@app.get("/session/{id}/location")
async def get_session_location(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/location")

@app.get("/session/{id}/owner")
async def get_session_owner(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/owner")

@app.get("/session/{id}/rules")
async def get_session_rules(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/rules")

@app.get("/session/{id}/created")
async def get_session_created_date(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/created")

# Session Activation Endpoints
@app.post("/session/{id}/activate")
async def activate_session(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/{id}/activate")

@app.post("/session/activate/user/{id}")
async def activate_session_user(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/user/{id}")

@app.post("/session/activate/challenge/{id}")
async def activate_session_challenge(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/challenge/{id}")

@app.post("/session/activate/recent/{id}")
async def activate_session_recent(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/recent/{id}")

@app.get("/session/activate/rule/{id}")
async def activate_session_rule(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/rule/{id}")

@app.post("/session/activate/time/{id}")
async def activate_session_time(request: Request, id: str):
    return await forward_request(request, SESSION_SERVICE_URL, f"session/activate/time/{id}")

# Session Listing Endpoints
@app.get("/session/existing")
async def get_existing_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing")

@app.get("/session/existing/open")
async def get_open_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/open")

@app.get("/session/existing/nearby")
async def get_nearby_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/nearby")

@app.get("/session/existing/private")
async def get_private_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/private")

@app.get("/session/existing/completed")
async def get_completed_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/completed")

@app.get("/session/existing/popular")
async def get_popular_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/popular")

@app.get("/session/existing/recently-updated")
async def get_recently_updated_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/recently-updated")

@app.get("/session/existing/joinable")
async def get_joinable_sessions(request: Request):
    return await forward_request(request, SESSION_SERVICE_URL, "session/existing/joinable")

@app.get("/session/existing/category/{type}")
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

if __name__ == "__main__":
    uvicorn.run(
        "app:app",
        host="0.0.0.0",
        port=8000,
        reload=True
    )