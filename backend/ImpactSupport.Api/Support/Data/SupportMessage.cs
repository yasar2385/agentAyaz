namespace ImpactSupport.Api.Support.Data
{
    public class SupportMessage
    {
        public long Id { get; set; }
        public string MessageId { get; set; } = "";
        public string SupportSessionId { get; set; } = "";
        public string SenderUserId { get; set; } = "";
        public string? SenderName { get; set; }
        public string? SenderRole { get; set; }
        public string MessageText { get; set; } = "";
        public string MessageType { get; set; } = "USER";
        public DateTime CreatedAtUtc { get; set; }

        public SupportSession? SupportSession { get; set; }
    }
}
