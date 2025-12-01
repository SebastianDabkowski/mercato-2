using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class LegalContentModel : PageModel
    {
        private readonly ILogger<LegalContentModel> _logger;
        private readonly LegalDocumentService _legalDocumentService;

        public IReadOnlyCollection<LegalDocumentViewModel> Documents { get; private set; } = Array.Empty<LegalDocumentViewModel>();
        public IReadOnlyCollection<SelectListItem> DocumentTypeOptions { get; private set; } = Array.Empty<SelectListItem>();
        public string? SuccessMessage { get; private set; }
        public string? ErrorMessage { get; private set; }

        [BindProperty]
        public CreateLegalDocumentInput NewDocument { get; set; } = new();

        [BindProperty]
        public EditLegalDocumentInput EditDocument { get; set; } = new();

        public LegalContentModel(
            ILogger<LegalContentModel> logger,
            LegalDocumentService legalDocumentService)
        {
            _logger = logger;
            _legalDocumentService = legalDocumentService;
        }

        public async Task<IActionResult> OnGetAsync(string? success = null, string? error = null)
        {
            SuccessMessage = success;
            ErrorMessage = error;

            await LoadDocumentsAsync();
            LoadDocumentTypeOptions();

            _logger.LogInformation(
                "Admin {UserId} viewed legal content management, found {DocumentCount} documents",
                GetUserId(),
                Documents.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDocumentsAsync();
                LoadDocumentTypeOptions();
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new CreateLegalDocumentCommand(
                NewDocument.DocumentType,
                NewDocument.Title!,
                NewDocument.Description,
                NewDocument.InitialContent,
                NewDocument.InitialVersionNumber,
                NewDocument.InitialEffectiveFrom,
                GetUserGuid());

            var result = await _legalDocumentService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} created legal document {DocumentId}: {Title}",
                    GetUserId(),
                    result.Document?.Id,
                    result.Document?.Title);

                return RedirectToPage(new { success = result.Message });
            }

            await LoadDocumentsAsync();
            LoadDocumentTypeOptions();
            ErrorMessage = string.Join(" ", result.Errors);
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDocumentsAsync();
                LoadDocumentTypeOptions();
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            var command = new UpdateLegalDocumentCommand(
                EditDocument.DocumentId,
                EditDocument.Title!,
                EditDocument.Description);

            var result = await _legalDocumentService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} updated legal document {DocumentId}: {Title}",
                    GetUserId(),
                    result.Document?.Id,
                    result.Document?.Title);

                return RedirectToPage(new { success = result.Message });
            }

            await LoadDocumentsAsync();
            LoadDocumentTypeOptions();
            ErrorMessage = string.Join(" ", result.Errors);
            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(Guid documentId)
        {
            var command = new ToggleLegalDocumentStatusCommand(documentId);
            var result = await _legalDocumentService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Admin {UserId} toggled status for legal document {DocumentId} to {IsActive}",
                    GetUserId(),
                    documentId,
                    result.Document?.IsActive);

                return RedirectToPage(new { success = result.Message });
            }

            return RedirectToPage(new { error = string.Join(" ", result.Errors) });
        }

        private async Task LoadDocumentsAsync()
        {
            var documents = await _legalDocumentService.HandleAsync(new GetAllLegalDocumentsQuery(true));

            Documents = documents.Select(d => new LegalDocumentViewModel
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                DocumentTypeName = d.DocumentTypeName,
                Title = d.Title,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                CurrentVersionNumber = d.CurrentVersion?.VersionNumber,
                CurrentVersionEffectiveFrom = d.CurrentVersion?.EffectiveFrom,
                ScheduledVersionNumber = d.ScheduledVersion?.VersionNumber,
                ScheduledVersionEffectiveFrom = d.ScheduledVersion?.EffectiveFrom,
                ScheduledVersionChangesSummary = d.ScheduledVersion?.ChangesSummary
            }).ToList();
        }

        private void LoadDocumentTypeOptions()
        {
            var existingTypes = Documents.Select(d => d.DocumentType).ToHashSet();

            var options = new List<SelectListItem>();
            foreach (LegalDocumentType type in Enum.GetValues(typeof(LegalDocumentType)))
            {
                if (!existingTypes.Contains(type))
                {
                    options.Add(new SelectListItem(GetDocumentTypeName(type), ((int)type).ToString()));
                }
            }

            DocumentTypeOptions = options;
        }

        private static string GetDocumentTypeName(LegalDocumentType documentType)
        {
            return documentType switch
            {
                LegalDocumentType.TermsOfService => "Terms of Service",
                LegalDocumentType.PrivacyPolicy => "Privacy Policy",
                LegalDocumentType.CookiePolicy => "Cookie Policy",
                LegalDocumentType.SellerAgreement => "Seller Agreement",
                _ => documentType.ToString()
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

        public class CreateLegalDocumentInput
        {
            [Required(ErrorMessage = "Document type is required")]
            public LegalDocumentType DocumentType { get; set; }

            [Required(ErrorMessage = "Title is required")]
            [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
            public string? Title { get; set; }

            [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
            public string? Description { get; set; }

            public string? InitialContent { get; set; }

            [StringLength(50, ErrorMessage = "Version number cannot exceed 50 characters")]
            public string? InitialVersionNumber { get; set; }

            public DateTime? InitialEffectiveFrom { get; set; }
        }

        public class EditLegalDocumentInput
        {
            [Required]
            public Guid DocumentId { get; set; }

            [Required(ErrorMessage = "Title is required")]
            [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
            public string? Title { get; set; }

            [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
            public string? Description { get; set; }
        }
    }
}
