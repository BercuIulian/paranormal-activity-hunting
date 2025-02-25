namespace UserManagement.API.DTOs
{
    public class SocialLoginDto
    {
        public string Provider { get; set; }  // e.g., "Google", "Facebook", "Twitter"
        public string Token { get; set; }     // OAuth token from the social provider
        public string Email { get; set; }     // Optional, some providers might include this
    }

    public class SocialLoginResponseDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string SocialProvider { get; set; }
        public string Token { get; set; }     // JWT token for your application
    }
}