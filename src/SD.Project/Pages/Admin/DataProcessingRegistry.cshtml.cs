using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Page model for managing the GDPR data processing activities registry.
/// </summary>
[RequireRole(UserRole.Admin)]
public class DataProcessingRegistryModel : PageModel
{
    private readonly ILogger<DataProcessingRegistryModel> _logger;
    private readonly DataProcessingRegistryService _registryService;

    /// <summary>
    /// GDPR Art. 6 legal bases for processing personal data.
    /// </summary>
    public static readonly IReadOnlyList<string> LegalBases = new[]
    {
        "Consent (Art. 6(1)(a))",
        "Contract (Art. 6(1)(b))",
        "Legal Obligation (Art. 6(1)(c))",
        "Vital Interests (Art. 6(1)(d))",
        "Public Task (Art. 6(1)(e))",
        "Legitimate Interests (Art. 6(1)(f))"
    };

    /// <summary>
    /// Static SelectListItem options for the legal basis dropdown.
    /// </summary>
    public static readonly IReadOnlyCollection<SelectListItem> LegalBasisOptions = CreateLegalBasisOptions();

    private static IReadOnlyCollection<SelectListItem> CreateLegalBasisOptions()
    {
        var items = new List<SelectListItem>
        {
            new SelectListItem("Select a legal basis...", "")
        };
        items.AddRange(LegalBases.Select(lb => new SelectListItem(lb, lb)));
        return items;
    }

    public DataProcessingRegistryModel(
        ILogger<DataProcessingRegistryModel> logger,
        DataProcessingRegistryService registryService)
    {
        _logger = logger;
        _registryService = registryService;
    }

    public IReadOnlyCollection<DataProcessingActivityViewModel> Activities { get; private set; } = Array.Empty<DataProcessingActivityViewModel>();

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowArchived { get; set; }

    [BindProperty]
    public CreateActivityInput NewActivity { get; set; } = new();

    [BindProperty]
    public EditActivityInput EditActivity { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
    {
        SuccessMessage = success;
        ErrorMessage = error;

        await LoadDataAsync();

        _logger.LogInformation(
            "Admin {UserId} viewed data processing registry, found {ActivityCount} activities",
            GetUserId(),
            Activities.Count);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            await LoadDataAsync();
            ErrorMessage = "User authentication error.";
            return Page();
        }

        var command = new CreateDataProcessingActivityCommand(
            NewActivity.Name!,
            NewActivity.Purpose!,
            NewActivity.LegalBasis!,
            NewActivity.DataCategories!,
            NewActivity.DataSubjects!,
            NewActivity.RetentionPeriod!,
            userId,
            NewActivity.Description,
            NewActivity.Processors,
            NewActivity.InternationalTransfers,
            NewActivity.SecurityMeasures);

        var result = await _registryService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} created data processing activity {ActivityId}: {ActivityName}",
                GetUserId(),
                result.Activity?.Id,
                result.Activity?.Name);

            return RedirectToPage(new { success = result.Message, showArchived = ShowArchived });
        }

        await LoadDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ErrorMessage = "Please correct the validation errors.";
            return Page();
        }

        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            await LoadDataAsync();
            ErrorMessage = "User authentication error.";
            return Page();
        }

        var command = new UpdateDataProcessingActivityCommand(
            EditActivity.Id,
            EditActivity.Name!,
            EditActivity.Purpose!,
            EditActivity.LegalBasis!,
            EditActivity.DataCategories!,
            EditActivity.DataSubjects!,
            EditActivity.RetentionPeriod!,
            userId,
            EditActivity.Description,
            EditActivity.Processors,
            EditActivity.InternationalTransfers,
            EditActivity.SecurityMeasures,
            EditActivity.ChangeReason);

        var result = await _registryService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} updated data processing activity {ActivityId}",
                GetUserId(),
                EditActivity.Id);

            return RedirectToPage(new { success = result.Message, showArchived = ShowArchived });
        }

        await LoadDataAsync();
        ErrorMessage = string.Join(" ", result.Errors);
        return Page();
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid activityId)
    {
        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "User authentication error.", showArchived = ShowArchived });
        }

        var command = new ArchiveDataProcessingActivityCommand(activityId, userId);
        var result = await _registryService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} archived data processing activity {ActivityId}",
                GetUserId(),
                activityId);

            return RedirectToPage(new { success = result.Message, showArchived = ShowArchived });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors), showArchived = ShowArchived });
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid activityId)
    {
        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "User authentication error.", showArchived = ShowArchived });
        }

        var command = new ReactivateDataProcessingActivityCommand(activityId, userId);
        var result = await _registryService.HandleAsync(command);

        if (result.Success)
        {
            _logger.LogInformation(
                "Admin {UserId} reactivated data processing activity {ActivityId}",
                GetUserId(),
                activityId);

            return RedirectToPage(new { success = result.Message, showArchived = ShowArchived });
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors), showArchived = ShowArchived });
    }

    public async Task<IActionResult> OnPostExportAsync(bool includeArchived)
    {
        var userId = GetUserIdGuid();
        if (userId == Guid.Empty)
        {
            return RedirectToPage(new { error = "User authentication error.", showArchived = ShowArchived });
        }

        var query = new ExportDataProcessingActivitiesQuery(includeArchived);
        var result = await _registryService.HandleAsync(query);

        if (result.Success && result.FileContent != null)
        {
            _logger.LogInformation(
                "Admin {UserId} exported data processing registry (includeArchived: {IncludeArchived})",
                GetUserId(),
                includeArchived);

            return File(result.FileContent, result.ContentType!, result.FileName);
        }

        return RedirectToPage(new { error = string.Join(" ", result.Errors), showArchived = ShowArchived });
    }

    private async Task LoadDataAsync()
    {
        var query = new GetAllDataProcessingActivitiesQuery(ShowArchived);
        var activities = await _registryService.HandleAsync(query);

        Activities = activities.Select(a => new DataProcessingActivityViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            Purpose = a.Purpose,
            LegalBasis = a.LegalBasis,
            DataCategories = a.DataCategories,
            DataSubjects = a.DataSubjects,
            Processors = a.Processors,
            RetentionPeriod = a.RetentionPeriod,
            InternationalTransfers = a.InternationalTransfers,
            SecurityMeasures = a.SecurityMeasures,
            IsActive = a.IsActive,
            CreatedByUserId = a.CreatedByUserId,
            CreatedByUserName = a.CreatedByUserName,
            LastModifiedByUserId = a.LastModifiedByUserId,
            LastModifiedByUserName = a.LastModifiedByUserName,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }

    private Guid GetUserIdGuid()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;
    }

    public class CreateActivityInput
    {
        [Required(ErrorMessage = "Activity name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        [StringLength(1000, ErrorMessage = "Purpose cannot exceed 1000 characters")]
        public string? Purpose { get; set; }

        [Required(ErrorMessage = "Legal basis is required")]
        [StringLength(500, ErrorMessage = "Legal basis cannot exceed 500 characters")]
        public string? LegalBasis { get; set; }

        [Required(ErrorMessage = "Data categories are required")]
        [StringLength(1000, ErrorMessage = "Data categories cannot exceed 1000 characters")]
        public string? DataCategories { get; set; }

        [Required(ErrorMessage = "Data subjects are required")]
        [StringLength(500, ErrorMessage = "Data subjects cannot exceed 500 characters")]
        public string? DataSubjects { get; set; }

        [StringLength(1000, ErrorMessage = "Processors cannot exceed 1000 characters")]
        public string? Processors { get; set; }

        [Required(ErrorMessage = "Retention period is required")]
        [StringLength(500, ErrorMessage = "Retention period cannot exceed 500 characters")]
        public string? RetentionPeriod { get; set; }

        [StringLength(1000, ErrorMessage = "International transfers cannot exceed 1000 characters")]
        public string? InternationalTransfers { get; set; }

        [StringLength(1000, ErrorMessage = "Security measures cannot exceed 1000 characters")]
        public string? SecurityMeasures { get; set; }
    }

    public class EditActivityInput
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Activity name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        [StringLength(1000, ErrorMessage = "Purpose cannot exceed 1000 characters")]
        public string? Purpose { get; set; }

        [Required(ErrorMessage = "Legal basis is required")]
        [StringLength(500, ErrorMessage = "Legal basis cannot exceed 500 characters")]
        public string? LegalBasis { get; set; }

        [Required(ErrorMessage = "Data categories are required")]
        [StringLength(1000, ErrorMessage = "Data categories cannot exceed 1000 characters")]
        public string? DataCategories { get; set; }

        [Required(ErrorMessage = "Data subjects are required")]
        [StringLength(500, ErrorMessage = "Data subjects cannot exceed 500 characters")]
        public string? DataSubjects { get; set; }

        [StringLength(1000, ErrorMessage = "Processors cannot exceed 1000 characters")]
        public string? Processors { get; set; }

        [Required(ErrorMessage = "Retention period is required")]
        [StringLength(500, ErrorMessage = "Retention period cannot exceed 500 characters")]
        public string? RetentionPeriod { get; set; }

        [StringLength(1000, ErrorMessage = "International transfers cannot exceed 1000 characters")]
        public string? InternationalTransfers { get; set; }

        [StringLength(1000, ErrorMessage = "Security measures cannot exceed 1000 characters")]
        public string? SecurityMeasures { get; set; }

        [StringLength(500, ErrorMessage = "Change reason cannot exceed 500 characters")]
        public string? ChangeReason { get; set; }
    }
}
