namespace ImpactSupport.Api.DTOs
{
    public sealed class CreateSupportRequestDto
    {
        public string UserId { get; set; } = "";
        public string? UserName { get; set; }
        public string UserRole { get; set; } = "";
        public string DocumentId { get; set; } = "";
        public string? DocumentLink { get; set; }
        public string? ImpactSessionId { get; set; }
        public string? ModuleName { get; set; }
        public string? ClientName { get; set; }
        public string Message { get; set; } = "";
        public string? CurrentUrl { get; set; }
    }
}
