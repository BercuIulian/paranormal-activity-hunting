using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserManagement.API.Models;
using UserManagement.API.DTOs;
using BCrypt.Net;
using System.Text.RegularExpressions;

namespace UserManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Challenge> _challenges;

        public UserController(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
            _challenges = database.GetCollection<Challenge>("Challenges");
        }

        // Registration Endpoints
        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register([FromBody] RegisterUserDto registerDto)
        {
            try
            {
                var existingUser = await _users.Find(u => u.Email == registerDto.Email).FirstOrDefaultAsync();
                if (existingUser != null)
                    return BadRequest(new { message = "User already exists" });

                var user = new User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    ExperiencePoints = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _users.InsertOneAsync(user);
                return Ok(MapToUserResponse(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("register/quick")]
        public async Task<ActionResult<UserResponseDto>> QuickRegister(QuickRegisterDto registerDto)
        {
            var user = new User
            {
                Username = registerDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _users.InsertOneAsync(user);
            return Ok(MapToUserResponse(user));
        }

        [HttpPost("register/validate-email")]
        public async Task<ActionResult> ValidateEmail(ValidateEmailDto emailDto)
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(emailDto.Email))
                return BadRequest(new { isValid = false, message = "Invalid email format" });

            var existingUser = await _users.Find(u => u.Email == emailDto.Email).FirstOrDefaultAsync();
            return Ok(new { isValid = existingUser == null, message = existingUser == null ? "Email is available" : "Email already registered" });
        }

        [HttpGet("register/check-username")]
        public async Task<ActionResult> CheckUsername([FromQuery] string username)
        {
            var existingUser = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
            return Ok(new { available = existingUser == null, message = existingUser == null ? "Username is available" : "Username is taken" });
        }

        [HttpPost("register/send-confirmation")]
        public async Task<ActionResult> SendConfirmation(ValidateEmailDto emailDto)
        {
            // Simulate sending email
            return Ok(new { success = true, message = "Confirmation email sent" });
        }

        [HttpPost("register/resend-confirmation")]
        public async Task<ActionResult> ResendConfirmation(ValidateEmailDto emailDto)
        {
            // Simulate resending email
            return Ok(new { success = true, message = "Confirmation email resent" });
        }

        [HttpPost("register/social")]
        public async Task<ActionResult> SocialRegister(SocialRegisterDto registerDto)
        {
            // Implement social registration
            return Ok(new { success = true, message = "Social registration successful" });
        }

        [HttpPost("register/phone")]
        public async Task<ActionResult> PhoneRegister([FromBody] PhoneRegisterDto registerDto)
        {
            try
            {
                var existingUser = await _users.Find(u => u.PhoneNumber == registerDto.PhoneNumber).FirstOrDefaultAsync();
                if (existingUser != null)
                    return BadRequest(new { message = "Phone number already registered" });

                if (registerDto.VerificationCode != "123456") // In real app, verify properly
                    return BadRequest(new { message = "Invalid verification code" });

                var user = new User
                {
                    Username = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    PhoneNumber = registerDto.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    ExperiencePoints = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _users.InsertOneAsync(user);
                return Ok(MapToUserResponse(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("register/admin")]
        public async Task<ActionResult> AdminRegister([FromBody] AdminRegisterDto registerDto)
        {
            try
            {
                var existingUser = await _users.Find(u => u.Email == registerDto.Email).FirstOrDefaultAsync();
                if (existingUser != null)
                    return BadRequest(new { message = "User already exists" });

                if (registerDto.AdminCode != "SECRET_ADMIN_CODE")
                    return Unauthorized(new { message = "Invalid admin code" });

                var user = new User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    IsAdmin = true,
                    ExperiencePoints = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _users.InsertOneAsync(user);
                return Ok(MapToUserResponse(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        //Login Endpoints
        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login(LoginUserDto loginDto)
        {
            var user = await _users.Find(u => u.Email == loginDto.Email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            return Ok(MapToUserResponse(user));
        }

        [HttpPost("login/admin")]
        public async Task<ActionResult> AdminLogin([FromBody] AdminLoginDto loginDto)
        {
            var user = await _users.Find(u => u.Email == loginDto.Email && u.IsAdmin).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid admin credentials");

            return Ok(MapToUserResponse(user));
        }

        [HttpPost("login/phone")]
        public async Task<ActionResult> PhoneLogin([FromBody] PhoneLoginDto loginDto)
        {
            var user = await _users.Find(u => u.PhoneNumber == loginDto.PhoneNumber).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            return Ok(MapToUserResponse(user));
        }

        [HttpPost("login/reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto resetDto)
        {
            var user = await _users.Find(u => u.Email == resetDto.Email).FirstOrDefaultAsync();
            if (user == null)
                return Ok(new { success = true, message = "If the email exists, a reset link will be sent" });

            // Simulate sending email
            return Ok(new { success = true, message = "Password reset link sent to your email" });
        }

        [HttpPost("login/email")]
        public async Task<ActionResult> EmailLogin([FromBody] EmailLoginDto loginDto)
        {
            var user = await _users.Find(u => u.Email == loginDto.Email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            return Ok(MapToUserResponse(user));
        }

        [HttpPost("login/secure-questions")]
        public async Task<ActionResult> SecureQuestions([FromBody] SecureQuestionsDto questionsDto)
        {
            // Generate a simple math question
            Random rnd = new Random();
            int num1 = rnd.Next(1, 20);
            int num2 = rnd.Next(1, 20);

            return Ok(new
            {
                question = $"What is {num1} + {num2}?",
                questionId = Guid.NewGuid().ToString(),
                answer = num1 + num2
            });
        }

        [HttpPost("login/quick")]
        public async Task<ActionResult> QuickLogin(QuickLoginDto loginDto)
        {
            var user = await _users.Find(u => u.Username == loginDto.Username).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            return Ok(MapToUserResponse(user));
        }

        [HttpPost("login/social")]
        public async Task<ActionResult> SocialLogin(SocialLoginDto loginDto)
        {
            // Implement social login
            return Ok(new { success = true, message = "Social login successful" });
        }

        [HttpGet("login/attempts")]
        public async Task<ActionResult> GetLoginAttempts([FromQuery] string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            return Ok(new { attempts = user.LoginAttempts });
        }

        // User Profile Endpoints
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            return Ok(MapToUserResponse(user));
        }

        [HttpGet("{id}/challenges-completed")]
        public async Task<ActionResult> GetCompletedChallengesForId(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            var completedChallenges = await _challenges
                .Find(c => user.CompletedChallenges.Contains(c.Id))
                .ToListAsync();

            return Ok(new { completedChallenges });
        }

        [HttpGet("{id}/inventory")]
        public async Task<ActionResult> GetInventory(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            return Ok(new { equipment = user.Equipment });
        }

        [HttpGet("{id}/created")]
        public async Task<ActionResult> GetCreatedDate(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            return Ok(new { createdAt = user.CreatedAt });
        }

        [HttpGet("{id}/updated")]
        public async Task<ActionResult> GetUpdatedDate(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            return Ok(new { updatedAt = user.UpdatedAt });
        }

        [HttpGet("{id}/admin-status")]
        public async Task<ActionResult> GetAdminStatus(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            return Ok(new { isAdmin = user.IsAdmin });
        }

        [HttpPut("{id}/update")]
        public async Task<ActionResult> UpdateProfile(string id, [FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return BadRequest(new { message = "Request body cannot be empty" });

                var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (user == null)
                    return NotFound(new { message = "User not found" });

                user.Username = updateDto.Username;
                user.UpdatedAt = DateTime.UtcNow;

                await _users.ReplaceOneAsync(u => u.Id == id, user);

                return Ok(new
                {
                    success = true,
                    message = "Profile modified successfully",
                    user = MapToUserResponse(user)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/profile")]
        public async Task<ActionResult> GetProfile(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            return Ok(MapToUserResponse(user));
        }

        [HttpGet("{id}/xp")]
        public async Task<ActionResult> GetXP(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            return Ok(new { xp = user.ExperiencePoints });
        }

        //Challenge Endpoints
        [HttpGet("challenges")]
        public async Task<ActionResult> GetAllChallenges()
        {
            var challenges = await _challenges.Find(_ => true).ToListAsync();
            return Ok(new { challenges });
        }        

        [HttpPost("challenges/complete")]
        public async Task<ActionResult> CompleteChallenge([FromBody] CompleteChallengeDto completeDto)
        {
            var challenge = await _challenges.Find(c => c.Id == completeDto.ChallengeId).FirstOrDefaultAsync();
            if (challenge == null)
                return NotFound("Challenge not found");

            var user = await _users.Find(u => u.Id == completeDto.UserId).FirstOrDefaultAsync();
            if (user == null)
                return NotFound("User not found");

            // Add challenge to user's completed challenges
            if (!user.CompletedChallenges.Contains(challenge.Id))
            {
                user.CompletedChallenges.Add(challenge.Id);
                user.ExperiencePoints += challenge.ExperiencePoints;
                await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
            }

            return Ok(new
            {
                success = true,
                message = "Challenge completed",
                experienceGained = challenge.ExperiencePoints
            });
        }

        [HttpGet("challenges/completed")]
        public async Task<ActionResult> GetCompletedChallenges([FromQuery] string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                return NotFound("User not found");

            // Get all challenges
            var allChallenges = await _challenges.Find(_ => true).ToListAsync();

            // Filter completed challenges
            var completedChallenges = allChallenges
                .Where(c => user.CompletedChallenges.Contains(c.Id))
                .ToList();

            return Ok(new { completedChallenges });
        }

        [HttpPost("challenges/start")]
        public async Task<ActionResult> StartChallenge([FromBody] StartChallengeDto startDto)
        {
            var challenge = await _challenges.Find(c => c.Id == startDto.ChallengeId).FirstOrDefaultAsync();
            if (challenge == null)
                return NotFound("Challenge not found");

            var user = await _users.Find(u => u.Id == startDto.UserId).FirstOrDefaultAsync();
            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                success = true,
                message = "Challenge started",
                startedAt = DateTime.UtcNow,
                challenge = new
                {
                    id = challenge.Id,
                    name = challenge.Name,
                    experiencePoints = challenge.ExperiencePoints
                }
            });
        }

        [HttpPost("challenges/assign")]
        public async Task<ActionResult> AssignChallenge([FromBody] AssignChallengeDto assignDto)
        {
            var challenge = await _challenges.Find(c => c.Id == assignDto.ChallengeId).FirstOrDefaultAsync();
            if (challenge == null)
                return NotFound("Challenge not found");

            var user = await _users.Find(u => u.Id == assignDto.UserId).FirstOrDefaultAsync();
            if (user == null)
                return NotFound("User not found");

            return Ok(new { success = true, message = "Challenge assigned successfully" });
        }

        [HttpGet("challenges/rewards")]
        public async Task<ActionResult> GetChallengeRewards([FromQuery] string challengeId)
        {
            var challenge = await _challenges.Find(c => c.Id == challengeId).FirstOrDefaultAsync();
            if (challenge == null)
                return NotFound("Challenge not found");

            return Ok(new
            {
                experiencePoints = challenge.Reward?.ExperiencePoints ?? 0,
                equipment = challenge.Reward?.Equipment ?? new List<string>()
            });
        }

        [HttpPost("challenges/add")]
        public async Task<ActionResult> AddChallenge([FromBody] CreateChallengeDto dto)
        {
            var challenge = new Challenge
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                ExperiencePoints = dto.ExperiencePoints,
                Reward = new ChallengeReward
                {
                    ExperiencePoints = dto.Reward?.ExperiencePoints ?? 0,
                    Equipment = dto.Reward?.Equipment ?? new List<string>()
                },
                CreatedAt = DateTime.UtcNow
            };

            await _challenges.InsertOneAsync(challenge);
            return Ok(challenge);
        }

        [HttpGet("challenges/daily")]
        public async Task<ActionResult> GetDailyChallenges()
        {
            var challenges = await _challenges
                .Find(c => c.Type == ChallengeType.Daily)
                .ToListAsync();
            return Ok(challenges);
        }

        [HttpGet("challenges/weekly")]
        public async Task<ActionResult> GetWeeklyChallenges()
        {
            var challenges = await _challenges
                .Find(c => c.Type == ChallengeType.Weekly)
                .ToListAsync();
            return Ok(challenges);
        }



        [HttpGet("status")]
        public ActionResult GetStatus()
        {
            return Ok(new { status = "healthy" });
        }

        private UserResponseDto MapToUserResponse(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsAdmin = user.IsAdmin,
                ExperiencePoints = user.ExperiencePoints,
                CompletedChallenges = user.CompletedChallenges,
                Equipment = user.Equipment
            };
        }
    }
}