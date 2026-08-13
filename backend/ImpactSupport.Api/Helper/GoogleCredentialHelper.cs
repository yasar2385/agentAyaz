using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using System.IO;

namespace ImpactSupport.Api.Helper
{
    public static class GoogleCredentialHelper
    {
        private static readonly string[] Scopes = new[]
        {
            "https://www.googleapis.com/auth/drive.readonly",
            "https://www.googleapis.com/auth/spreadsheets.readonly"
        };

        public enum CredentialType
        {
            Jwt,
            ServiceAccount
        }

        public static GoogleCredential CreateCredential(CredentialType credentialType, string credentialPath)
        {
            if (!File.Exists(credentialPath))
            {
                throw new FileNotFoundException(
                    $"Credential file not found: {credentialPath}");
            }

            try
            {
                using var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read);

                if (credentialType == CredentialType.Jwt)
                {
                    return CreateJwtCredential(stream);
                }
                else if (credentialType == CredentialType.ServiceAccount)
                {
                    return CreateServiceAccountCredential(stream);
                }
                else
                {
                    throw new ArgumentException("Invalid credential type");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create credential", ex);
            }
        }

        private static GoogleCredential CreateJwtCredential(Stream stream)
        {
            var serviceAccountCredential = CredentialFactory.FromStream<ServiceAccountCredential>(stream);
            return serviceAccountCredential.ToGoogleCredential().CreateScoped(Scopes);
        }

        private static GoogleCredential CreateServiceAccountCredential(Stream stream)
        {
            var serviceAccountCredential = CredentialFactory.FromStream<ServiceAccountCredential>(stream);
            return serviceAccountCredential.ToGoogleCredential().CreateScoped(Scopes);
        }
    }
}