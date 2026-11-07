using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class ConsumptionController : Controller
    {
        private readonly IConsumptionService _consumption;
        private readonly ISiteService _sites;
        private readonly IMaterialService _materials;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConsumptionController(IConsumptionService consumption, ISiteService sites,
            IMaterialService materials, UserManager<ApplicationUser> userManager)
        {
            _consumption = consumption;
            _sites = sites;
            _materials = materials;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? siteId, int? materialId, DateTime? from, DateTime? to)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
            ViewBag.SiteId = siteId;
            ViewBag.MaterialId = materialId;
            ViewBag.From = from;
            ViewBag.To = to;
            return View(await _consumption.GetAllAsync(siteId, materialId, from, to));
        }

        public async Task<IActionResult> Details(int id)
        {
            var record = await _consumption.GetByIdAsync(id);
            if (record == null) return NotFound();
            return View(record);
        }

        public async Task<IActionResult> Record()
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
            return View(new MaterialConsumption { ConsumptionDate = DateTime.Today });
        }

        [HttpPost]
        public async Task<IActionResult> Record(MaterialConsumption model)
        {
            var user = await _userManager.GetUserAsync(User);

            ModelState.Remove(nameof(MaterialConsumption.Site));
            ModelState.Remove(nameof(MaterialConsumption.Material));
            ModelState.Remove(nameof(MaterialConsumption.RecordedBy));

            if (!ModelState.IsValid)
            {
                ViewBag.Sites = await _sites.GetAllAsync();
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }

            var created = await _consumption.RecordAsync(model, user!.Id);
            TempData["Success"] = "Consumption recorded and inventory updated.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
    }
}
