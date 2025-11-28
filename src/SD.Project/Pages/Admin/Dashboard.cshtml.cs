using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Admin
{
    [RequireRole(UserRole.Admin)]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ILogger<DashboardModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Admin dashboard accessed by user {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        }
    }
}
