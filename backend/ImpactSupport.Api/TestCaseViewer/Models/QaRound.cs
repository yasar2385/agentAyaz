namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class QaRound
{
    public int RoundNumber { get; set; }
    public string QaStatus { get; set; } = string.Empty;
    public string DevStatus { get; set; } = string.Empty;
}
