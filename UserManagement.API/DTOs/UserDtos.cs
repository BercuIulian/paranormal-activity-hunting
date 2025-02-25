namespace UserManagement.API.DTOs
{
    public class RegisterUserDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class UserResponseDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAdmin { get; set; }
        public int ExperiencePoints { get; set; }
        public List<string> CompletedChallenges { get; set; }
        public List<string> Equipment { get; set; }
    }

    public class AdminLoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    
    public class ResetPasswordDto
    {
        public string Email { get; set; }
    }   

    public class SecureQuestionsDto
    {
        public string Email { get; set; }
    }

    public class UpdateProfileDto
    {
        public string Username { get; set; }
    }

    public class StartChallengeDto
    {
        public string ChallengeId { get; set; }
        public string UserId { get; set; }
    }   
}