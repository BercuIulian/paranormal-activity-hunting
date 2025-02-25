using SessionManagement.API.DTOs;
using SessionManagement.API.Models;

namespace SessionManagement.API.DTOs
{
    public class SessionDetailsDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public string Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatorId { get; set; }
        public ParanormalCategory Category { get; set; }
        public int ParticipantCount { get; set; }
    }

    public class SessionParticipantDto
    {
        public string UserId { get; set; }
        public ParticipantRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class SessionLogDto
    {
        public DateTime Timestamp { get; set; }
        public LogType Type { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
    }

    public class SessionLocationDto
    {
        public string Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}