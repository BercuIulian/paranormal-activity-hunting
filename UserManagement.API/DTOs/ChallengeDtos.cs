using UserManagement.API.Models;

namespace UserManagement.API.DTOs
{
    public class CreateChallengeDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ChallengeType Type { get; set; }
        public int ExperiencePoints { get; set; }
        public ChallengeRewardDto Reward { get; set; }
    }

    public class ChallengeRewardDto
    {
        public int ExperiencePoints { get; set; }
        public List<string> Equipment { get; set; }
    }

    public class AssignChallengeDto
    {
        public string ChallengeId { get; set; }
        public string UserId { get; set; }
    }

    public class CompleteChallengeDto
    {
        public string ChallengeId { get; set; }
        public string UserId { get; set; }
    }
}