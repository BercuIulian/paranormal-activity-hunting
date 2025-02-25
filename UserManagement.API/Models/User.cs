using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserManagement.API.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool IsPhoneConfirmed { get; set; }
        public bool IsAdmin { get; set; }
        public int ExperiencePoints { get; set; }
        public List<string> CompletedChallenges { get; set; } = new();
        public List<string> Equipment { get; set; } = new();
        public List<LoginAttempt> LoginAttempts { get; set; } = new();
        public string SocialProvider { get; set; } // e.g., "Google", "Facebook"
        public string SocialId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }        
    }

    public class LoginAttempt
    {
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}