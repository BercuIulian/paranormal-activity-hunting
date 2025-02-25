namespace UserManagement.API.DTOs
{
    public class QuickLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class EmailLoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class PhoneLoginDto
    {
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }

    public class SecurityQuestionDto
    {
        public string Question { get; set; }
        public int Answer { get; set; }
    }

    public class LoginAttemptsResponseDto
    {
        public List<LoginAttemptDto> Attempts { get; set; }
    }

    public class LoginAttemptDto
    {
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string IpAddress { get; set; }
    }
}