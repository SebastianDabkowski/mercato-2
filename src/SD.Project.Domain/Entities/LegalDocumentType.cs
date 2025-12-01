namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the types of legal documents that can be managed.
/// </summary>
public enum LegalDocumentType
{
    /// <summary>
    /// Terms of Service document.
    /// </summary>
    TermsOfService = 1,

    /// <summary>
    /// Privacy Policy document.
    /// </summary>
    PrivacyPolicy = 2,

    /// <summary>
    /// Cookie Policy document.
    /// </summary>
    CookiePolicy = 3,

    /// <summary>
    /// Seller Agreement document.
    /// </summary>
    SellerAgreement = 4
}
