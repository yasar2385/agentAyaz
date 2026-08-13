using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using ImpactSupport.Api.TestCaseViewer.Options;
namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class GoogleCredentialProvider : IGoogleCredentialProvider
{
    private static readonly string[] Scopes =
    {
        "https://www.googleapis.com/auth/drive.readonly",
        "https://www.googleapis.com/auth/spreadsheets.readonly"
    };

    private readonly GoogleCredential _credential;

    public GoogleCredentialProvider(IOptions<GoogleOptions> options, IWebHostEnvironment env)
    {
        var settings = options.Value;

        var fullPath = Path.IsPathRooted(settings.CredentialsPath)
            ? settings.CredentialsPath
            : Path.Combine(env.ContentRootPath, settings.CredentialsPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Google credential file not found: {fullPath}");
        }

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        // Type-checked load — rejects anything that isn't actually a service account key,
        // instead of silently accepting any credential type (the FromStream obsolete warning).
        var serviceAccountCredential = CredentialFactory.FromStream<ServiceAccountCredential>(stream);

        _credential = serviceAccountCredential.ToGoogleCredential().CreateScoped(Scopes);
    }

    public GoogleCredential GetCredential() => _credential;
}