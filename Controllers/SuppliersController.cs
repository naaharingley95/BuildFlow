using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _suppliers;
        private readonly IMaterialService _materials;

        public SuppliersController(ISupplierService suppliers, IMaterialService materials)
        {
            _suppliers = suppliers;
            _materials = materials;
        }

        public async Task<IActionResult> Index(string? search, SupplierStatus? status)
        {
            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(await _suppliers.GetAllAsync(search, status));
        }

        public async Task<IActionResult> Details(int id)
        {
            try { return View(await _suppliers.GetDetailsAsync(id)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [Authorize(Roles = "Admin,Procurement")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
            return View(new Supplier());
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Create(Supplier model)
        {
            ModelState.Remove(nameof(Supplier.SupplierMaterials));
            ModelState.Remove(nameof(Supplier.PurchaseOrders));
            if (!ModelState.IsValid)
            {
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }
            await _suppliers.CreateAsync(model);
            TempData["Success"] = $"Supplier '{model.CompanyName}' created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Procurement")]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _suppliers.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
            return View(supplier);
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Supplier model)
        {
            if (id != model.Id) return BadRequest();
            ModelState.Remove(nameof(Supplier.SupplierMaterials));
            ModelState.Remove(nameof(Supplier.PurchaseOrders));
            if (!ModelState.IsValid)
            {
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }
            await _suppliers.UpdateAsync(model);
            TempData["Success"] = $"Supplier '{model.CompanyName}' updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
