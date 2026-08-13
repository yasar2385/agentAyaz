namespace ImpactSupport.Api.Support.DTOs
{
    public class CreateSupportRequestResponse
    {
        public bool Created { get; set; }
        public string SupportSessionId { get; set; } = "";
        public string TicketNo { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
