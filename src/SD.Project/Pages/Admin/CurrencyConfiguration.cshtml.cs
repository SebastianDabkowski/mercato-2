using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Page model for managing currencies and platform currency settings.
/// </summary>
[RequireRole(UserRole.Admin)]
public class CurrencyConfigurationModel : PageModel
{
    private readonly ILogger<CurrencyConfigurationModel> _logger;
    private readonly CurrencyService _currencyService;

    /// <summary>
    /// Common ISO 4217 currencies for reference.
    /// </summary>
    public static readonly IReadOnlyList<(string Code, string Name, string Symbol, int Decimals)> CommonCurrencies = new List<(string, string, string, int)>
    {
        ("EUR", "Euro", "€", 2),
        ("USD", "US Dollar", "$", 2),
        ("GBP", "British Pound", "£", 2),
        ("PLN", "Polish Zloty", "zł", 2),
        ("CHF", "Swiss Franc", "CHF", 2),
        ("SEK", "Swedish Krona", "kr", 2),
        ("NOK", "Norwegian Krone", "kr", 2),
        ("DKK", "Danish Krone", "kr", 2),
        ("CZK", "Czech Koruna", "Kč", 2),
        ("HUF", "Hungarian Forint", "Ft", 0),
        ("RON", "Romanian Leu", "lei", 2),
        ("BGN", "Bulgarian Lev", "лв", 2),
        ("HRK", "Croatian Kuna", "kn", 2),
        ("JPY", "Japanese Yen", "¥", 0),
        ("CNY", "Chinese Yuan", "¥", 2),
        ("INR", "Indian Rupee", "₹", 2),
        ("AUD", "Australian Dollar", "$", 2),
        ("CAD", "Canadian Dollar", "$", 2),
        ("BRL", "Brazilian Real", "R$", 2),
        ("MXN", "Mexican Peso", "$", 2)
    };

    public CurrencyConfigurationModel(
        ILogger<CurrencyConfigurationModel> logger,
        CurrencyService currencyService)
    {
        _logger = logger;
        _currencyService = currencyService;
    }

    public IReadOnlyCollection<CurrencyViewModel> Currencies { get; private set; } = Array.Empty<CurrencyViewModel>();
    public CurrencyViewModel? BaseCurrency { get; private set; }

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? WarningMessage { get; private set; }
    public bool ShowConfirmationModal { get; private set; }
    public Guid? PendingBaseCurrencyId { get; private set; }

    [BindProperty]
    public CreateCurrencyInput NewCurrency { get; set; } = new();

    [BindProperty]
    public EditCurrencyInput EditCurrency { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(
        string? success = null,
        string? error = null,
        string? warning = null,
        Guid? confirmBaseCurrencyId = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;
        WarningMessage = warning;

        if (confirmBaseCurrencyId.HasValue)
        {
            ShowConfirmationModal = true;
            PendingBaseCurrencyId = confirmBaseCurrencyId;
        }

        await LoadDataAsync();

        _logger.LogInformation(
            "Admin viewed currency configuration, found {CurrencyCount} currencies",
            Currencies.Count);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        // Log all ModelState entries for debugging
        _logger.LogInformation("ModelState entries before cleanup: {Entries}",
            string.Join("; ", ModelState.Keys));

        // Remove validation errors from EditCurrency since we're only creating
        foreach (var key in ModelState.Keys.ToList())
        {
            if (key.StartsWith("EditCurrency"))
            {
                ModelState.Remove(key);
            }
        }

        _logger.LogInformation("ModelState entries after cleanup: {Entries}",
            string.Join("; ", ModelState.Keys));
        _logger.LogInformation("NewCurrency values: Code={Code}, Name={Name}, Symbol={Symbol}",
            NewCurrency.Code, NewCurrency.Name, NewCurrency.Symbol);

        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            var errorDetails = string.Join("; ", ModelState
                .Where(m => m.Value?.Errors.Count > 0)
                .Select(m => $"{m.Key}: {string.Join(", ", m.Value!.Errors.Select(e => e.ErrorMessage))}"));
            ErrorMessage = string.IsNullOrEmpty(errorDetails)
                ? "Please correct the validation errors."
                : $"Validation errors: {errorDetails}";
            return Page();
        }

        var command = new CreateCurrencyCommand(
            NewCurrency.Code!,
            NewCurrency.Name!,
            NewCurrency.Symbol!,
            NewCurrency.DecimalPlaces,
            NewCurrency.DisplayOrder);

        var result = await _currencyService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin created currency {CurrencyCode}: {CurrencyName}",
                result.Currency?.Code,
                result.Currency?.Name);

            return RedirectToPage(new { success = result.Message });
        }

        await LoadDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        // Remove validation errors from NewCurrency since we're only editing
        foreach (var key in ModelState.Keys.ToList())
        {
            if (key.StartsWith("NewCurrency"))
            {
                ModelState.Remove(key);
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var command = new UpdateCurrencyCommand(
            EditCurrency.CurrencyId,
            EditCurrency.Name!,
            EditCurrency.Symbol!,
            EditCurrency.DecimalPlaces,
            EditCurrency.DisplayOrder);

        var result = await _currencyService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin updated currency {CurrencyId}",
                EditCurrency.CurrencyId);

            return RedirectToPage(new { success = result.Message });
        }

        await LoadDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid currencyId)
    {
        // First get the current currency to check its status
        var query = new GetCurrencyByIdQuery(currencyId);
        var currency = await _currencyService.HandleAsync(query);

        if (currency is null)
        {
            return RedirectToPage(new { error = "Currency not found." });
        }

        var result = currency.IsEnabled
            ? await _currencyService.HandleAsync(new DisableCurrencyCommand(currencyId))
            : await _currencyService.HandleAsync(new EnableCurrencyCommand(currencyId));

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin toggled status for currency {CurrencyId} to {IsEnabled}",
                currencyId,
                result.Currency?.IsEnabled);

            return RedirectToPage(new { success = result.Message });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    public async Task<IActionResult> OnPostSetBaseCurrencyAsync(Guid currencyId, bool confirmed = false)
    {
        var command = new SetBaseCurrencyCommand(currencyId, confirmed);
        var result = await _currencyService.HandleAsync(command);

        if (result.RequiresConfirmation)
        {
            _logger.LogInformation(
                "Admin initiated base currency change to {CurrencyId}, requires confirmation",
                currencyId);

            return RedirectToPage(new
            {
                warning = result.WarningMessage,
                confirmBaseCurrencyId = currencyId
            });
        }

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin changed base currency to {CurrencyCode}",
                result.Currency?.Code);

            return RedirectToPage(new { success = result.Message });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    public async Task<IActionResult> OnPostUpdateExchangeRateAsync(Guid currencyId, decimal rate, string? source)
    {
        var command = new UpdateExchangeRateCommand(currencyId, rate, source);
        var result = await _currencyService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin updated exchange rate for {CurrencyId} to {Rate}",
                currencyId,
                rate);

            return RedirectToPage(new { success = result.Message });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors) });
    }

    private async Task LoadDataAsync()
    {
        var currencies = await _currencyService.HandleAsync(new GetAllCurrenciesQuery());
        Currencies = currencies.Select(c => new CurrencyViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Symbol = c.Symbol,
            DecimalPlaces = c.DecimalPlaces,
            IsEnabled = c.IsEnabled,
            IsBaseCurrency = c.IsBaseCurrency,
            ExchangeRate = c.ExchangeRate,
            ExchangeRateUpdatedAt = c.ExchangeRateUpdatedAt,
            ExchangeRateSource = c.ExchangeRateSource,
            DisplayOrder = c.DisplayOrder,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        BaseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
    }

    public class CreateCurrencyInput
    {
        [Required(ErrorMessage = "Currency code is required")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be 3 characters")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Symbol is required")]
        [StringLength(10, ErrorMessage = "Symbol cannot exceed 10 characters")]
        public string? Symbol { get; set; }

        [Range(0, 8, ErrorMessage = "Decimal places must be between 0 and 8")]
        public int DecimalPlaces { get; set; } = 2;

        public int DisplayOrder { get; set; } = 0;
    }

    public class EditCurrencyInput
    {
        [Required]
        public Guid CurrencyId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Symbol is required")]
        [StringLength(10, ErrorMessage = "Symbol cannot exceed 10 characters")]
        public string? Symbol { get; set; }

        [Range(0, 8, ErrorMessage = "Decimal places must be between 0 and 8")]
        public int DecimalPlaces { get; set; } = 2;

        public int DisplayOrder { get; set; } = 0;
    }
}
