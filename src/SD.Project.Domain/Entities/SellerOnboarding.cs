namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a seller's onboarding progress and store profile data.
/// Tracks the multi-step wizard state and all collected information.
/// </summary>
public class SellerOnboarding
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    
    // Wizard progress tracking
    public OnboardingStep CurrentStep { get; private set; }
    public OnboardingStatus Status { get; private set; }
    
    // Step 1: Store Profile
    public string? StoreName { get; private set; }
    public string? StoreDescription { get; private set; }
    public string? StoreAddress { get; private set; }
    public string? StoreCity { get; private set; }
    public string? StorePostalCode { get; private set; }
    public string? StoreCountry { get; private set; }
    public bool StoreProfileCompleted { get; private set; }
    
    // Step 2: Verification Data
    public SellerType SellerType { get; private set; }
    
    // Company verification fields
    public string? BusinessName { get; private set; }
    public string? BusinessRegistrationNumber { get; private set; }
    public string? TaxIdentificationNumber { get; private set; }
    public string? BusinessAddress { get; private set; }
    public string? ContactPersonName { get; private set; }
    public string? ContactPersonEmail { get; private set; }
    public string? ContactPersonPhone { get; private set; }
    
    // Individual verification fields
    public string? FullName { get; private set; }
    public string? PersonalIdNumber { get; private set; }
    public string? PersonalAddress { get; private set; }
    public string? PersonalEmail { get; private set; }
    public string? PersonalPhone { get; private set; }
    
    public bool VerificationCompleted { get; private set; }
    
    // Step 3: Payout Settings
    public string? BankAccountHolder { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankName { get; private set; }
    public string? BankSwiftCode { get; private set; }
    public bool PayoutCompleted { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    private SellerOnboarding()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new seller onboarding record for the specified user.
    /// </summary>
    public SellerOnboarding(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        CurrentStep = OnboardingStep.StoreProfile;
        Status = OnboardingStatus.InProgress;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the store profile step data.
    /// </summary>
    public void UpdateStoreProfile(
        string storeName,
        string storeDescription,
        string storeAddress,
        string storeCity,
        string storePostalCode,
        string storeCountry)
    {
        if (Status != OnboardingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot update completed onboarding.");
        }

        StoreName = storeName?.Trim();
        StoreDescription = storeDescription?.Trim();
        StoreAddress = storeAddress?.Trim();
        StoreCity = storeCity?.Trim();
        StorePostalCode = storePostalCode?.Trim();
        StoreCountry = storeCountry?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the store profile step as completed and advances to the next step.
    /// </summary>
    public void CompleteStoreProfile()
    {
        if (Status != OnboardingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot update completed onboarding.");
        }

        ValidateStoreProfile();
        StoreProfileCompleted = true;
        CurrentStep = OnboardingStep.Verification;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the verification step data for company sellers.
    /// </summary>
    public void UpdateCompanyVerification(
        string businessName,
        string businessRegistrationNumber,
        string taxIdentificationNumber,
        string businessAddress,
        string contactPersonName,
        string contactPersonEmail,
        string contactPersonPhone)
    {
        if (Status != OnboardingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot update completed onboarding.");
        }

        SellerType = SellerType.Company;
        BusinessName = businessName?.Trim();
        BusinessRegistrationNumber = businessRegistrationNumber?.Trim();
        TaxIdentificationNumber = taxIdentificationNumber?.Trim();
        BusinessAddress = businessAddress?.Trim();
        ContactPersonName = contactPersonName?.Trim();
        ContactPersonEmail = contactPersonEmail?.Trim();
        ContactPersonPhone = contactPersonPhone?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the verification step data for individual sellers.
    /// </summary>
    public void UpdateIndividualVerification(
        string fullName,
        string personalIdNumber,
        string personalAddress,
        string personalEmail,
        string personalPhone)
    {
        if (Status != OnboardingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot update completed onboarding.");
        }

        SellerType = SellerType.Individual;
        FullName = fullName?.Trim();
        PersonalIdNumber = personalIdNumber?.Trim();
        PersonalAddress = personalAddress?.Trim();
        PersonalEmail = personalEmail?.Trim();
        PersonalPhone = personalPhone?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the verification step data (legacy - for backwards compatibility).
    /// Only works for Company seller type.
    /// </summary>
    [Obsolete("Use UpdateCompanyVerification or UpdateIndividualVerification instead.")]
    public void UpdateVerification(
        string businessName,
        string businessRegistrationNumber,
        string taxIdentificationNumber,
        string businessAddress)
    {
        if (SellerType == SellerType.Individual)
        {
            throw new InvalidOperationException("Cannot use legacy UpdateVerification for Individual sellers. Use UpdateIndividualVerification instead.");
        }

        UpdateCompanyVerification(
            businessName,
            businessRegistrationNumber,
            taxIdentificationNumber,
            businessAddress,
            ContactPersonName ?? string.Empty,
            ContactPersonEmail ?? string.Empty,
            ContactPersonPhone ?? string.Empty);
    }

    /// <summary>
    /// Marks the verification step as completed and advances to the next step.
    /// </summary>
    public void CompleteVerification()
    {
        if (Status != OnboardingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot update completed onboarding.");
        }

        if (!StoreProfileCompleted)
        {
            throw new InvalidOperationException("Store profile must be completed first.");
        }

        ValidateVerification();
        VerificationCompleted = true;
        CurrentStep = OnboardingStep.Payout;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the payout step data.
    /// </summary>
    public void UpdatePayout(
        string bankAccountHolder,
        string bankAccountNumber,
        string bankName,
        string bankSwiftCode)
    {
        if (Status != OnboardingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot update completed onboarding.");
        }

        BankAccountHolder = bankAccountHolder?.Trim();
        BankAccountNumber = bankAccountNumber?.Trim();
        BankName = bankName?.Trim();
        BankSwiftCode = bankSwiftCode?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the payout step as completed and submits the onboarding.
    /// </summary>
    public void CompletePayout()
    {
        if (Status != OnboardingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot update completed onboarding.");
        }

        if (!StoreProfileCompleted)
        {
            throw new InvalidOperationException("Store profile must be completed first.");
        }

        if (!VerificationCompleted)
        {
            throw new InvalidOperationException("Verification must be completed first.");
        }

        ValidatePayout();
        PayoutCompleted = true;
        CurrentStep = OnboardingStep.Completed;
        Status = OnboardingStatus.PendingVerification;
        SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves the seller onboarding after admin verification.
    /// </summary>
    public void Approve()
    {
        if (Status != OnboardingStatus.PendingVerification)
        {
            throw new InvalidOperationException("Onboarding must be pending verification to approve.");
        }

        Status = OnboardingStatus.Verified;
        VerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the seller onboarding after admin review.
    /// </summary>
    public void Reject()
    {
        if (Status != OnboardingStatus.PendingVerification)
        {
            throw new InvalidOperationException("Onboarding must be pending verification to reject.");
        }

        Status = OnboardingStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the list of validation errors for the store profile step.
    /// </summary>
    public IReadOnlyList<string> GetStoreProfileErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(StoreName))
            errors.Add("Store name is required.");

        if (string.IsNullOrWhiteSpace(StoreDescription))
            errors.Add("Store description is required.");

        if (string.IsNullOrWhiteSpace(StoreAddress))
            errors.Add("Store address is required.");

        if (string.IsNullOrWhiteSpace(StoreCity))
            errors.Add("Store city is required.");

        if (string.IsNullOrWhiteSpace(StorePostalCode))
            errors.Add("Store postal code is required.");

        if (string.IsNullOrWhiteSpace(StoreCountry))
            errors.Add("Store country is required.");

        return errors;
    }

    /// <summary>
    /// Gets the list of validation errors for the verification step.
    /// </summary>
    public IReadOnlyList<string> GetVerificationErrors()
    {
        var errors = new List<string>();

        if (SellerType == SellerType.NotSpecified)
        {
            errors.Add("Please select your seller type (Company or Individual).");
            return errors;
        }

        if (SellerType == SellerType.Company)
        {
            if (string.IsNullOrWhiteSpace(BusinessName))
                errors.Add("Company name is required.");

            if (string.IsNullOrWhiteSpace(BusinessRegistrationNumber))
                errors.Add("Business registration number is required.");

            if (string.IsNullOrWhiteSpace(TaxIdentificationNumber))
                errors.Add("Tax identification number is required.");

            if (string.IsNullOrWhiteSpace(BusinessAddress))
                errors.Add("Registered business address is required.");

            if (string.IsNullOrWhiteSpace(ContactPersonName))
                errors.Add("Contact person name is required.");

            if (string.IsNullOrWhiteSpace(ContactPersonEmail))
                errors.Add("Contact person email is required.");
            else if (!IsValidEmail(ContactPersonEmail))
                errors.Add("Contact person email format is invalid.");

            if (string.IsNullOrWhiteSpace(ContactPersonPhone))
                errors.Add("Contact person phone is required.");
        }
        else if (SellerType == SellerType.Individual)
        {
            if (string.IsNullOrWhiteSpace(FullName))
                errors.Add("Full name is required.");

            if (string.IsNullOrWhiteSpace(PersonalIdNumber))
                errors.Add("Personal ID number is required.");

            if (string.IsNullOrWhiteSpace(PersonalAddress))
                errors.Add("Address is required.");

            if (string.IsNullOrWhiteSpace(PersonalEmail))
                errors.Add("Email is required.");
            else if (!IsValidEmail(PersonalEmail))
                errors.Add("Email format is invalid.");

            if (string.IsNullOrWhiteSpace(PersonalPhone))
                errors.Add("Phone number is required.");
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the list of validation errors for the payout step.
    /// </summary>
    public IReadOnlyList<string> GetPayoutErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BankAccountHolder))
            errors.Add("Bank account holder name is required.");

        if (string.IsNullOrWhiteSpace(BankAccountNumber))
            errors.Add("Bank account number is required.");

        if (string.IsNullOrWhiteSpace(BankName))
            errors.Add("Bank name is required.");

        if (string.IsNullOrWhiteSpace(BankSwiftCode))
            errors.Add("Bank SWIFT/BIC code is required.");

        return errors;
    }

    private void ValidateStoreProfile()
    {
        var errors = GetStoreProfileErrors();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }
    }

    private void ValidateVerification()
    {
        var errors = GetVerificationErrors();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }
    }

    private void ValidatePayout()
    {
        var errors = GetPayoutErrors();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }
    }
}
