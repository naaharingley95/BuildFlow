using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize(Roles = "Admin,Procurement")]
    public class PurchaseOrdersController : Controller
    {
        private readonly IPurchaseOrderService _orders;
        private readonly ISupplierService _suppliers;
        private readonly ISiteService _sites;
        private readonly IMaterialService _materials;
        private readonly IMaterialRequestService _requests;
        private readonly UserManager<ApplicationUser> _userManager;

        public PurchaseOrdersController(
            IPurchaseOrderService orders, ISupplierService suppliers,
            ISiteService sites, IMaterialService materials,
            IMaterialRequestService requests, UserManager<ApplicationUser> userManager)
        {
            _orders = orders;
            _suppliers = suppliers;
            _sites = sites;
            _materials = materials;
            _requests = requests;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index(int? siteId, PurchaseOrderStatus? status)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.SiteId = siteId;
            ViewBag.Status = status;
            return View(await _orders.GetAllAsync(siteId, status));
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var po = await _orders.GetByIdAsync(id);
            if (po == null) return NotFound();
            return View(po);
        }

        public async Task<IActionResult> Create(int? materialRequestId)
        {
            ViewBag.Suppliers = await _suppliers.GetAllAsync(null, SupplierStatus.Active);
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
            ViewBag.MaterialRequestId = materialRequestId;

            var model = new PurchaseOrder
            {
                OrderDate = DateTime.Today,
                ExpectedDeliveryDate = DateTime.Today.AddDays(14)
            };

            if (materialRequestId.HasValue)
            {
                var req = await _requests.GetByIdAsync(materialRequestId.Value);
                if (req != null)
                {
                    model.SiteId = req.SiteId;
                    model.MaterialRequestId = req.Id;
                    ViewBag.MaterialRequest = req;
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PurchaseOrder model, int[] materialIds, decimal[] quantities, decimal[] unitPrices, int? materialRequestId)
        {
            var user = await _userManager.GetUserAsync(User);
            model.CreatedByUserId = user!.Id;
            if (materialRequestId.HasValue) model.MaterialRequestId = materialRequestId;

            var items = new List<PurchaseOrderItem>();
            for (int i = 0; i < materialIds.Length; i++)
            {
                if (materialIds[i] > 0 && quantities[i] > 0)
                {
                    items.Add(new PurchaseOrderItem
                    {
                        MaterialId = materialIds[i],
                        OrderedQuantity = quantities[i],
                        UnitPrice = unitPrices[i]
                    });
                }
            }

            if (items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "At least one item is required.");
                ViewBag.Suppliers = await _suppliers.GetAllAsync(null, SupplierStatus.Active);
                ViewBag.Sites = await _sites.GetAllAsync();
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }

            ModelState.Remove(nameof(PurchaseOrder.Supplier));
            ModelState.Remove(nameof(PurchaseOrder.Site));
            ModelState.Remove(nameof(PurchaseOrder.CreatedBy));
            ModelState.Remove(nameof(PurchaseOrder.MaterialRequest));
            ModelState.Remove(nameof(PurchaseOrder.Items));
            ModelState.Remove(nameof(PurchaseOrder.Deliveries));

            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = await _suppliers.GetAllAsync(null, SupplierStatus.Active);
                ViewBag.Sites = await _sites.GetAllAsync();
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }

            try
            {
                var created = await _orders.CreateAsync(model, items);
                TempData["Success"] = $"Purchase Order {created.OrderNumber} created.";
                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Suppliers = await _suppliers.GetAllAsync(null, SupplierStatus.Active);
                ViewBag.Sites = await _sites.GetAllAsync();
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Issue(int id)
        {
            var ok = await _orders.IssueAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Purchase Order issued." : "Cannot issue.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            try
            {
                var ok = await _orders.CancelAsync(id, reason);
                TempData[ok ? "Success" : "Error"] = ok ? "Purchase Order cancelled." : "Cannot cancel.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
