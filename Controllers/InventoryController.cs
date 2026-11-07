using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventory;
        private readonly ISiteService _sites;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(IInventoryService inventory, ISiteService sites, UserManager<ApplicationUser> userManager)
        {
            _inventory = inventory;
            _sites = sites;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? siteId)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.SiteId = siteId;

            if (siteId.HasValue)
                return View(await _inventory.GetSiteInventoryAsync(siteId.Value));

            return View(new List<SiteInventory>());
        }

        public async Task<IActionResult> LowStock()
            => View(await _inventory.GetLowStockAsync());

        public async Task<IActionResult> Transactions(int siteInventoryId)
            => View(await _inventory.GetTransactionsAsync(siteInventoryId));

        [Authorize(Roles = "Admin,StoreManager")]
        [HttpPost]
        public async Task<IActionResult> Adjust(int siteId, int materialId, decimal quantity, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            await _inventory.RecordAdjustmentAsync(siteId, materialId, quantity, reason, user!.Id);
            TempData["Success"] = "Inventory adjustment recorded.";
            return RedirectToAction(nameof(Index), new { siteId });
        }
    }
}
