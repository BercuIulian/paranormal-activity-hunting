namespace SessionManagement.API.Models
{
    public class SessionChallenge
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string ChallengeId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ChallengeStatus Status { get; set; }

        public virtual Session Session { get; set; }
    }

    public enum ChallengeStatus
    {
        Assigned,
        InProgress,
        Completed,
        Failed,
        Expired
    }
}