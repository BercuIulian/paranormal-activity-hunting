namespace SessionManagement.API.Models
{
    public class SessionParticipant
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string UserId { get; set; }
        public ParticipantRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public ParticipantStatus Status { get; set; }

        public virtual Session Session { get; set; }
    }

    public enum ParticipantRole
    {
        Leader,
        Investigator,
        Observer,
        Equipment_Manager,
        EVP_Specialist,
        Medium
    }

    public enum ParticipantStatus
    {
        Invited,
        Joined,
        Left,
        Kicked,
        Banned
    }
}