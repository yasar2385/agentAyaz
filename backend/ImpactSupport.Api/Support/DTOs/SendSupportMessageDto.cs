namespace ImpactSupport.Api.Support.DTOs
{
    public class SendSupportMessageDto
    {
        public string SenderUserId { get; set; } = "";
        public string? SenderName { get; set; }
        public string? SenderRole { get; set; }
        public string Message { get; set; } = "";
    }
}
