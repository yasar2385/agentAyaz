namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class GoogleFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTimeOffset? ModifiedTime { get; set; }
}