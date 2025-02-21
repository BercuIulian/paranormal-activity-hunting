using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserManagement.API.Models;
using UserManagement.API.DTOs;
using BCrypt.Net;

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

        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(RegisterUserDto registerDto)
        {
            // Check if user exists
            var existingUser = await _users.Find(u => u.Email == registerDto.Email).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest("User already exists");

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

        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login(LoginUserDto loginDto)
        {
            var user = await _users.Find(u => u.Email == loginDto.Email).FirstOrDefaultAsync();
            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            return Ok(MapToUserResponse(user));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(string id)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            return Ok(MapToUserResponse(user));
        }

        [HttpGet("challenges")]
        public async Task<ActionResult<List<Challenge>>> GetChallenges()
        {
            var challenges = await _challenges.Find(_ => true).ToListAsync();
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
                ExperiencePoints = user.ExperiencePoints,
                CompletedChallenges = user.CompletedChallenges,
                Equipment = user.Equipment
            };
        }
    }
}