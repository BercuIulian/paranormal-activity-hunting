using SessionManagement.API.DTOs;
using SessionManagement.API.Models;

namespace SessionManagement.API.DTOs
{
    public class QuickSessionDto
    {
        public string Title { get; set; }
        public string Location { get; set; }
        public string CreatorId { get; set; }
        public ParanormalCategory Category { get; set; }
    }

    public class PrivateSessionDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string CreatorId { get; set; }
        public List<string> InvitedUserIds { get; set; }
        public ParanormalCategory Category { get; set; }
    }

    public class ScheduledSessionDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string CreatorId { get; set; }
        public DateTime ScheduledStartTime { get; set; }
        public DateTime ScheduledEndTime { get; set; }
        public int MaxParticipants { get; set; }
        public ParanormalCategory Category { get; set; }
    }

    public class GroupSessionDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string CreatorId { get; set; }
        public int MaxParticipants { get; set; }
        public List<RequiredEquipmentDto> RequiredEquipment { get; set; }
        public ParanormalCategory Category { get; set; }
    }
}