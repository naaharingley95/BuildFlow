using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class SitesController : Controller
    {
        private readonly ISiteService _sites;

        public SitesController(ISiteService sites) => _sites = sites;

        public async Task<IActionResult> Index()
            => View(await _sites.GetAllAsync(activeOnly: false));

        public async Task<IActionResult> Details(int id)
        {
            try { return View(await _sites.GetSiteDetailsAsync(id)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new ConstructionSite { StartDate = DateTime.Today });

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(ConstructionSite model)
        {
            ModelState.Remove(nameof(ConstructionSite.SiteInventories));
            ModelState.Remove(nameof(ConstructionSite.MaterialRequests));
            ModelState.Remove(nameof(ConstructionSite.PurchaseOrders));
            ModelState.Remove(nameof(ConstructionSite.Deliveries));
            ModelState.Remove(nameof(ConstructionSite.MaterialConsumptions));
            ModelState.Remove(nameof(ConstructionSite.SiteManager));

            if (!ModelState.IsValid) return View(model);
            await _sites.CreateAsync(model);
            TempData["Success"] = $"Site '{model.SiteName}' created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var site = await _sites.GetByIdAsync(id);
            if (site == null) return NotFound();
            return View(site);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, ConstructionSite model)
        {
            if (id != model.Id) return BadRequest();
            ModelState.Remove(nameof(ConstructionSite.SiteInventories));
            ModelState.Remove(nameof(ConstructionSite.MaterialRequests));
            ModelState.Remove(nameof(ConstructionSite.PurchaseOrders));
            ModelState.Remove(nameof(ConstructionSite.Deliveries));
            ModelState.Remove(nameof(ConstructionSite.MaterialConsumptions));
            ModelState.Remove(nameof(ConstructionSite.SiteManager));

            if (!ModelState.IsValid) return View(model);
            await _sites.UpdateAsync(model);
            TempData["Success"] = $"Site '{model.SiteName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _sites.DeactivateAsync(id);
            TempData["Success"] = "Site deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
