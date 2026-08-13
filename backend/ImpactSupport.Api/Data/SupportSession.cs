namespace ImpactSupport.Api.Data
{
    public class SupportSession
    {
        public long Id { get; set; }
        public string SupportSessionId { get; set; } = "";
        public string TicketNo { get; set; } = "";
        public string UserId { get; set; } = "";
        public string? UserName { get; set; }
        public string UserRole { get; set; } = "";
        public string DocumentId { get; set; } = "";
        public string? DocumentLink { get; set; }
        public string? ImpactSessionId { get; set; }
        public string? ModuleName { get; set; }
        public string? ClientName { get; set; }
        public string? CurrentUrl { get; set; }
        public string Status { get; set; } = "OPEN";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? ClosedAtUtc { get; set; }

        public List<SupportMessage> Messages { get; set; } = new();
    }
}
