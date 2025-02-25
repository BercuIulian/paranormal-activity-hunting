using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserManagement.API.Models
{
    public class Challenge
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public int ExperiencePoints { get; set; }
        public string Difficulty { get; set; }
        public ChallengeType Type { get; set; }
        public List<string> Requirements { get; set; } = new();
        public List<string> AssignedUsers { get; set; } = new();
        public List<string> CompletedByUsers { get; set; } = new();
        public ChallengeReward Reward { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public enum ChallengeType
    {
        Daily = 0,
        Weekly = 1,
        Special = 2,
        Event = 3
    }

    public class ChallengeReward
    {
        public int ExperiencePoints { get; set; }
        public List<string> Equipment { get; set; } = new();
    }
}