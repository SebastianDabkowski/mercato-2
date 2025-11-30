namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the result of applying a promo code.
/// </summary>
public sealed record ApplyPromoCodeResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? PromoCodeId,
    string? PromoCode,
    decimal DiscountAmount,
    string? DiscountDescription,
    decimal NewTotalAmount)
{
    public static ApplyPromoCodeResultDto Success(
        Guid promoCodeId,
        string promoCode,
        decimal discountAmount,
        string discountDescription,
        decimal newTotalAmount)
        => new(true, null, promoCodeId, promoCode, discountAmount, discountDescription, newTotalAmount);

    public static ApplyPromoCodeResultDto Failed(string errorMessage)
        => new(false, errorMessage, null, null, 0m, null, 0m);

    public static ApplyPromoCodeResultDto AlreadyApplied(string appliedCode)
        => new(false, $"A promo code is already applied ({appliedCode}). Remove it first to apply a different code.", null, null, 0m, null, 0m);
}

/// <summary>
/// DTO representing the result of removing a promo code.
/// </summary>
public sealed record RemovePromoCodeResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    decimal NewTotalAmount)
{
    public static RemovePromoCodeResultDto Success(decimal newTotalAmount)
        => new(true, null, newTotalAmount);

    public static RemovePromoCodeResultDto Failed(string errorMessage)
        => new(false, errorMessage, 0m);

    public static RemovePromoCodeResultDto NoPromoApplied()
        => new(false, "No promo code is currently applied.", 0m);
}

/// <summary>
/// DTO representing applied promo code information for display.
/// </summary>
public sealed record AppliedPromoCodeDto(
    Guid PromoCodeId,
    string PromoCode,
    decimal DiscountAmount,
    string DiscountDescription);
