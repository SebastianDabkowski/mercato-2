using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin;

/// <summary>
/// Page model for viewing audit history of a data processing activity.
/// </summary>
[RequireRole(UserRole.Admin)]
public class DataProcessingHistoryModel : PageModel
{
    private readonly ILogger<DataProcessingHistoryModel> _logger;
    private readonly DataProcessingRegistryService _registryService;

    public DataProcessingHistoryModel(
        ILogger<DataProcessingHistoryModel> logger,
        DataProcessingRegistryService registryService)
    {
        _logger = logger;
        _registryService = registryService;
    }

    public DataProcessingActivityViewModel? Activity { get; private set; }
    public IReadOnlyCollection<DataProcessingActivityAuditLogViewModel> AuditLogs { get; private set; } = Array.Empty<DataProcessingActivityAuditLogViewModel>();

    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid ActivityId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (ActivityId == Guid.Empty)
        {
            ErrorMessage = "Activity ID is required.";
            return Page();
        }

        var activityQuery = new GetDataProcessingActivityByIdQuery(ActivityId);
        var activity = await _registryService.HandleAsync(activityQuery);

        if (activity is null)
        {
            ErrorMessage = "Data processing activity not found.";
            return Page();
        }

        Activity = new DataProcessingActivityViewModel
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description,
            Purpose = activity.Purpose,
            LegalBasis = activity.LegalBasis,
            DataCategories = activity.DataCategories,
            DataSubjects = activity.DataSubjects,
            Processors = activity.Processors,
            RetentionPeriod = activity.RetentionPeriod,
            InternationalTransfers = activity.InternationalTransfers,
            SecurityMeasures = activity.SecurityMeasures,
            IsActive = activity.IsActive,
            CreatedByUserId = activity.CreatedByUserId,
            CreatedByUserName = activity.CreatedByUserName,
            LastModifiedByUserId = activity.LastModifiedByUserId,
            LastModifiedByUserName = activity.LastModifiedByUserName,
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt
        };

        var auditQuery = new GetDataProcessingActivityAuditLogsQuery(ActivityId);
        var auditLogs = await _registryService.HandleAsync(auditQuery);

        AuditLogs = auditLogs.Select(a => new DataProcessingActivityAuditLogViewModel
        {
            Id = a.Id,
            DataProcessingActivityId = a.DataProcessingActivityId,
            UserId = a.UserId,
            UserName = a.UserName,
            Action = a.Action,
            ChangeReason = a.ChangeReason,
            CreatedAt = a.CreatedAt
        }).ToList();

        _logger.LogInformation(
            "Admin {UserId} viewed audit history for data processing activity {ActivityId}",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
            ActivityId);

        return Page();
    }
}
