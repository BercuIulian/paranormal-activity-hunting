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
        public List<string> Requirements { get; set; } = new();
    }
}