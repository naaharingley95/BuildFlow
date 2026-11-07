using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboard;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(IDashboardService dashboard, UserManager<ApplicationUser> userManager)
        {
            _dashboard = dashboard;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var summary = await _dashboard.GetDashboardSummaryAsync();
            return View(summary);
        }
    }
}
