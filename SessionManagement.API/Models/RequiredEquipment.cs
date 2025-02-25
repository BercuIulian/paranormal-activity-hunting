namespace SessionManagement.API.Models
{
    public class RequiredEquipment
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string EquipmentName { get; set; }
        public string Description { get; set; }
        public bool IsMandatory { get; set; }
        public int Quantity { get; set; }

        public virtual Session Session { get; set; }
    }
}