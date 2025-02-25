using SessionManagement.API.DTOs;
using SessionManagement.API.Models;

namespace SessionManagement.API.DTOs
{
    public class CreateSessionDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string CreatorId { get; set; }
    }

    public class SessionResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; }
        public List<string> ParticipantIds { get; set; }
        public string CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}