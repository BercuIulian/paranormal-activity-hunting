namespace SessionManagement.API.Models
{
    public class SessionRule
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsMandatory { get; set; }
        public RuleCategory Category { get; set; }

        public virtual Session Session { get; set; }
    }

    public enum RuleCategory
    {
        Safety = 0,
        Equipment = 1,
        Communication = 2,
        Investigation = 3,
        Behavior = 4,
        Other = 5
    }
}