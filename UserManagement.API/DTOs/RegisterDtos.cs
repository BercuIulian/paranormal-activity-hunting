namespace UserManagement.API.DTOs
{
    public class QuickRegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ValidateEmailDto
    {
        public string Email { get; set; }
    }

    public class CheckUsernameDto
    {
        public string Username { get; set; }
    }

    public class SocialRegisterDto
    {
        public string Provider { get; set; } // "Google", "Facebook", etc.
        public string Token { get; set; }
        public string Email { get; set; }
    }

    public class PhoneRegisterDto
    {
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string VerificationCode { get; set; }
    }

    public class AdminRegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string AdminCode { get; set; }
    }
}