namespace ImpactSupport.Api.TestCaseViewer.Options;


public sealed class GoogleOptions
{
    public string CredentialsPath { get; set; } = string.Empty;

    public string MasterFolderId { get; set; } = string.Empty;

    public string RegressionFolderId { get; set; } = string.Empty;

    public string RegressionFilePrefix { get; set; } = "Regression testing _";
}