namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the supported external login providers for OAuth/OIDC authentication.
/// </summary>
public enum ExternalLoginProvider
{
    /// <summary>
    /// No external provider - traditional email/password authentication.
    /// </summary>
    None = 0,

    /// <summary>
    /// Google OAuth login.
    /// </summary>
    Google = 1,

    /// <summary>
    /// Facebook OAuth login.
    /// </summary>
    Facebook = 2,

    /// <summary>
    /// Apple Sign In (reserved for future implementation).
    /// </summary>
    Apple = 3
}
