using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SessionManagement.API.Models
{
    public class Session
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public SessionType Type { get; set; }

        public bool IsPrivate { get; set; }

        [Required]
        public SessionStatus Status { get; set; }

        // Location details
        public string Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Timing
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }

        // Participants
        [Required]
        public string CreatorId { get; set; }
        public int MaxParticipants { get; set; }
        public virtual List<SessionParticipant> Participants { get; set; } = new();

        // Features
        public virtual List<SessionChallenge> Challenges { get; set; } = new();
        public virtual List<SessionRule> Rules { get; set; } = new();
        public virtual List<SessionLog> Logs { get; set; } = new();

        // Statistics
        public int ViewCount { get; set; }
        public int JoinRequestCount { get; set; }
        public double Rating { get; set; }

        // Category
        public ParanormalCategory Category { get; set; }
        public DifficultyLevel Difficulty { get; set; }

        // Equipment requirements
        public virtual List<RequiredEquipment> RequiredEquipment { get; set; } = new();
    }

    public enum SessionType
    {
        Quick,
        Scheduled,
        Private,
        Test,
        Group
    }

    public enum SessionStatus
    {
        Created,
        Scheduled,
        Active,
        Paused,
        Completed,
        Cancelled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ParanormalCategory
    {
        GhostHunt,
        CryptidSearch,
        UFOWatch,
        PsychicInvestigation,
        DemonicInvestigation,
        PoltergeistActivity,
        EVPSession,
        Other
    }

    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }
}