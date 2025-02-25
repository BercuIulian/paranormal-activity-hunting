using SessionManagement.API.DTOs;
using SessionManagement.API.Models;

namespace SessionManagement.API.DTOs
{
    public class SessionRuleDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsMandatory { get; set; }
        public RuleCategory Category { get; set; }
    }

    public class RequiredEquipmentDto
    {
        public string EquipmentName { get; set; }
        public string Description { get; set; }
        public bool IsMandatory { get; set; }
        public int Quantity { get; set; }
    }
}