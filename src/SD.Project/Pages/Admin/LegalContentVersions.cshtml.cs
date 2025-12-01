using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin)]
    public class LegalContentVersionsModel : PageModel
    {
        private readonly ILogger<LegalContentVersionsModel> _logger;
        private readonly LegalDocumentService _legalDocumentService;

        public LegalDocumentViewModel? Document { get; private set; }
        public IReadOnlyCollection<LegalDocumentVersionViewModel> Versions { get; private set; } = Array.Empty<LegalDocumentVersionViewModel>();
        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        [BindProperty]
        public CreateVersionInput NewVersion { get; set; } = new();

        [BindProperty]
        public EditVersionInput EditVersion { get; set; } = new();

        public LegalContentVersionsModel(
            ILogger<LegalContentVersionsModel> logger,
            LegalDocumentService legalDocumentService)
        {
            _logger = logger;
            _legalDocumentService = legalDocumentService;
        }

        public async Task<IActionResult> OnGetAsync(Guid documentId, string? success = null, string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;

            var documentResult = await _legalDocumentService.HandleAsync(new GetLegalDocumentByIdQuery(documentId));
            if (documentResult is null)
            {
                return RedirectToPage("/Admin/LegalContent", new { error = "Legal document not found." });
            }

            Document = MapToViewModel(documentResult);
            NewVersion.LegalDocumentId = documentId;

            await LoadVersionsAsync(documentId);

            _logger.LogInformation(
                "Admin {UserId} viewed versions for legal document {DocumentId}, found {VersionCount} versions",
                GetUserId(),
                documentId,
                Versions.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostCreateVersionAsync()
        {
            var documentId = NewVersion.LegalDocumentId;

            if (!ModelState.IsValid)
            {
                await LoadDocumentAndVersionsAsync(documentId);
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new CreateLegalDocumentVersionCommand(
                documentId,
                NewVersion.VersionNumber!,
                NewVersion.Content!,
                NewVersion.EffectiveFrom,
                NewVersion.ChangesSummary,
                GetUserGuid());

            var result = await _legalDocumentService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} created version {VersionNumber} for legal document {DocumentId}",
                    GetUserId(),
                    result.Version?.VersionNumber,
                    documentId);

                return RedirectToPage(new { documentId, success = result.Message });
            }

            await LoadDocumentAndVersionsAsync(documentId);
            ErrorMessage = string.Join(" ", result.Errors);
            return Page();
        }

        public async Task<IActionResult> OnPostEditVersionAsync()
        {
            var documentId = EditVersion.LegalDocumentId;

            if (!ModelState.IsValid)
            {
                await LoadDocumentAndVersionsAsync(documentId);
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new UpdateLegalDocumentVersionCommand(
                EditVersion.VersionId,
                EditVersion.Content!,
                EditVersion.EffectiveFrom,
                EditVersion.ChangesSummary);

            var result = await _legalDocumentService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} updated version {VersionId} for legal document {DocumentId}",
                    GetUserId(),
                    EditVersion.VersionId,
                    documentId);

                return RedirectToPage(new { documentId, success = result.Message });
            }

            await LoadDocumentAndVersionsAsync(documentId);
            ErrorMessage = string.Join(" ", result.Errors);
            return Page();
        }

        public async Task<IActionResult> OnPostPublishVersionAsync(Guid documentId, Guid versionId)
        {
            var command = new PublishLegalDocumentVersionCommand(versionId);
            var result = await _legalDocumentService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} published version {VersionId} for legal document {DocumentId}",
                    GetUserId(),
                    versionId,
                    documentId);

                return RedirectToPage(new { documentId, success = result.Message });
            }

            return RedirectToPage(new { documentId, error = string.Join(" ", result.Errors) });
        }

        private async Task LoadDocumentAndVersionsAsync(Guid documentId)
        {
            var documentResult = await _legalDocumentService.HandleAsync(new GetLegalDocumentByIdQuery(documentId));
            if (documentResult is not null)
            {
                Document = MapToViewModel(documentResult);
            }
            NewVersion.LegalDocumentId = documentId;
            await LoadVersionsAsync(documentId);
        }

        private async Task LoadVersionsAsync(Guid documentId)
        {
            var versions = await _legalDocumentService.HandleAsync(new GetLegalDocumentVersionsQuery(documentId));

            Versions = versions.Select(v => new LegalDocumentVersionViewModel
            {
                Id = v.Id,
                LegalDocumentId = v.LegalDocumentId,
                VersionNumber = v.VersionNumber,
                Content = v.Content,
                ChangesSummary = v.ChangesSummary,
                EffectiveFrom = v.EffectiveFrom,
                EffectiveTo = v.EffectiveTo,
                IsPublished = v.IsPublished,
                IsCurrentlyActive = v.IsCurrentlyActive,
                IsScheduled = v.IsScheduled,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            }).ToList();
        }

        private static LegalDocumentViewModel MapToViewModel(LegalDocumentDto dto)
        {
            return new LegalDocumentViewModel
            {
                Id = dto.Id,
                DocumentType = dto.DocumentType,
                DocumentTypeName = dto.DocumentTypeName,
                Title = dto.Title,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                CurrentVersionNumber = dto.CurrentVersion?.VersionNumber,
                CurrentVersionEffectiveFrom = dto.CurrentVersion?.EffectiveFrom,
                ScheduledVersionNumber = dto.ScheduledVersion?.VersionNumber,
                ScheduledVersionEffectiveFrom = dto.ScheduledVersion?.EffectiveFrom,
                ScheduledVersionChangesSummary = dto.ScheduledVersion?.ChangesSummary
            };
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }

        private Guid? GetUserGuid()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var guid) ? guid : null;
        }

        public class CreateVersionInput
        {
            public Guid LegalDocumentId { get; set; }

            [Required(ErrorMessage = "Version number is required")]
            [StringLength(50, MinimumLength = 1, ErrorMessage = "Version number must be between 1 and 50 characters")]
            public string? VersionNumber { get; set; }

            [Required(ErrorMessage = "Content is required")]
            public string? Content { get; set; }

            [Required(ErrorMessage = "Effective date is required")]
            public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

            [StringLength(500, ErrorMessage = "Changes summary cannot exceed 500 characters")]
            public string? ChangesSummary { get; set; }
        }

        public class EditVersionInput
        {
            public Guid LegalDocumentId { get; set; }

            [Required]
            public Guid VersionId { get; set; }

            [Required(ErrorMessage = "Content is required")]
            public string? Content { get; set; }

            [Required(ErrorMessage = "Effective date is required")]
            public DateTime EffectiveFrom { get; set; }

            [StringLength(500, ErrorMessage = "Changes summary cannot exceed 500 characters")]
            public string? ChangesSummary { get; set; }
        }
    }
}
