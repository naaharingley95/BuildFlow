using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class MaterialsController : Controller
    {
        private readonly IMaterialService _materials;

        public MaterialsController(IMaterialService materials) => _materials = materials;

        public async Task<IActionResult> Index(string? search, int? categoryId, bool? isActive)
        {
            ViewBag.Categories = await _materials.GetCategoriesAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.IsActive = isActive;
            return View(await _materials.GetAllAsync(search, categoryId, isActive));
        }

        public async Task<IActionResult> Details(int id)
        {
            var material = await _materials.GetByIdAsync(id);
            if (material == null) return NotFound();
            return View(material);
        }

        [Authorize(Roles = "Admin,Procurement")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _materials.GetCategoriesAsync();
            return View(new Material());
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Create(Material model)
        {
            ModelState.Remove(nameof(Material.Category));
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _materials.GetCategoriesAsync();
                return View(model);
            }
            await _materials.CreateAsync(model);
            TempData["Success"] = $"Material '{model.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Procurement")]
        public async Task<IActionResult> Edit(int id)
        {
            var material = await _materials.GetByIdAsync(id);
            if (material == null) return NotFound();
            ViewBag.Categories = await _materials.GetCategoriesAsync();
            return View(material);
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Material model)
        {
            if (id != model.Id) return BadRequest();
            ModelState.Remove(nameof(Material.Category));
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _materials.GetCategoriesAsync();
                return View(model);
            }
            await _materials.UpdateAsync(model);
            TempData["Success"] = $"Material '{model.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _materials.DeleteAsync(id);
            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Material deleted."
                : "Cannot delete — material is in use.";
            return RedirectToAction(nameof(Index));
        }
    }
}
