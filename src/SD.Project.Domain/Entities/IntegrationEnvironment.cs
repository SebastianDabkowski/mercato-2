namespace SD.Project.Domain.Entities;

/// <summary>
/// Environment in which an integration operates.
/// </summary>
public enum IntegrationEnvironment
{
    /// <summary>Sandbox/testing environment.</summary>
    Sandbox,

    /// <summary>Production environment.</summary>
    Production
}
