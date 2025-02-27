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

    public class TestSessionDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string CreatorId { get; set; }
    }

    public class SessionChallengesDto
    {
        public Guid SessionId { get; set; }
        public List<ChallengeDto> Challenges { get; set; }
    }

    public class SessionChallengeDto
    {
        public string ChallengeId { get; set; }
    }

    public class ChallengeDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PointValue { get; set; }
    }

    public class SessionRulesDto
    {
        public Guid SessionId { get; set; }
        public List<RuleDto> Rules { get; set; }
    }

    public class RuleDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsMandatory { get; set; }
        public string Category { get; set; }
    }

    public class SessionTimeDto
    {
        public DateTime StartTime { get; set; }
        public int Duration { get; set; } // in minutes
    }
}