namespace SessionManagement.API.Models
{
    public class Session
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime ScheduledTime { get; set; }
        public SessionStatus Status { get; set; }
        public List<string> ParticipantIds { get; set; } = new();
        public string CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum SessionStatus
    {
        Scheduled,
        Active,
        Completed,
        Cancelled
    }
}