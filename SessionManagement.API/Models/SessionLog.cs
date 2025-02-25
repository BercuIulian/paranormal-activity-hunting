namespace SessionManagement.API.Models
{
    public class SessionLog
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public LogType Type { get; set; }
        public string Description { get; set; }
        public string? MetaData { get; set; }

        public virtual Session Session { get; set; }
    }

    public enum LogType
    {
        ParticipantJoined,
        ParticipantLeft,
        ActivityRecorded,
        EquipmentReading,
        EVPRecorded,
        PhotoTaken,
        TemperatureChange,
        EMFSpike,
        Note
    }
}