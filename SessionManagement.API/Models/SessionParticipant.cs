using System.Text.Json.Serialization;

namespace SessionManagement.API.Models
{
    public class SessionParticipant
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string UserId { get; set; }
        public ParticipantRole Role { get; set; }
        public ParticipantStatus Status { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        public Session Session { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ParticipantRole
    {
        Leader,
        Investigator,
        Observer,
        Medium,
        TechSpecialist,
        SecurityTeam
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