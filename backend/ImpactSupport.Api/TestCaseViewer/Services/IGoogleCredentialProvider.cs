using Google.Apis.Auth.OAuth2;

namespace ImpactSupport.Api.TestCaseViewer.Services;


public interface IGoogleCredentialProvider
{
    GoogleCredential GetCredential();
}