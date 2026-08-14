namespace ImpactSupport.Api.TestCaseViewer.Options;

public sealed class MongoAuthOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string UsersCollectionName { get; set; } = "users";
    public string UsernameField { get; set; } = "username";
    public string PasswordField { get; set; } = "password";
    public string PasswordHashField { get; set; } = "passwordHash";
    public string DisplayNameField { get; set; } = "displayName";
    public string RoleField { get; set; } = "role";
    public string EmailField { get; set; } = "email";
    public string IsActiveField { get; set; } = "isActive";
}
