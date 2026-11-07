using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class DeliveriesController : Controller
    {
        private readonly IDeliveryService _deliveries;
        private readonly IPurchaseOrderService _orders;
        private readonly ISiteService _sites;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeliveriesController(IDeliveryService deliveries, IPurchaseOrderService orders,
            ISiteService sites, UserManager<ApplicationUser> userManager)
        {
            _deliveries = deliveries;
            _orders = orders;
            _sites = sites;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? siteId, DeliveryStatus? status)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.SiteId = siteId;
            ViewBag.Status = status;
            return View(await _deliveries.GetAllAsync(siteId, status));
        }

        public async Task<IActionResult> Details(int id)
        {
            var delivery = await _deliveries.GetByIdAsync(id);
            if (delivery == null) return NotFound();
            return View(delivery);
        }

        [Authorize(Roles = "Admin,Procurement")]
        public async Task<IActionResult> Create(int purchaseOrderId)
        {
            var po = await _orders.GetByIdAsync(purchaseOrderId);
            if (po == null) return NotFound();
            ViewBag.PurchaseOrder = po;
            return View(new Delivery
            {
                PurchaseOrderId = purchaseOrderId,
                SiteId = po.SiteId,
                SupplierId = po.SupplierId,
                ExpectedArrivalDate = DateTime.Today.AddDays(3)
            });
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Create(Delivery model, int[] poItemIds, decimal[] orderedQtys, int[] materialIds)
        {
            var items = new List<DeliveryItem>();
            for (int i = 0; i < poItemIds.Length; i++)
            {
                if (orderedQtys[i] > 0)
                {
                    items.Add(new DeliveryItem
                    {
                        PurchaseOrderItemId = poItemIds[i],
                        MaterialId = materialIds[i],
                        OrderedQuantity = orderedQtys[i]
                    });
                }
            }

            ModelState.Remove(nameof(Delivery.Site));
            ModelState.Remove(nameof(Delivery.Supplier));
            ModelState.Remove(nameof(Delivery.PurchaseOrder));
            ModelState.Remove(nameof(Delivery.Items));

            if (!ModelState.IsValid)
            {
                ViewBag.PurchaseOrder = await _orders.GetByIdAsync(model.PurchaseOrderId);
                return View(model);
            }

            var created = await _deliveries.CreateAsync(model, items);
            TempData["Success"] = $"Delivery {created.DeliveryNumber} scheduled.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmReceipt(int id, int[] itemIds, decimal[] receivedQtys, decimal[] rejectedQtys, string?[] remarksArr)
        {
            var user = await _userManager.GetUserAsync(User);
            var receipts = new List<(int, decimal, decimal, string?)>();
            for (int i = 0; i < itemIds.Length; i++)
                receipts.Add((itemIds[i], receivedQtys[i], rejectedQtys[i], i < remarksArr.Length ? remarksArr[i] : null));

            var ok = await _deliveries.ConfirmReceiptAsync(id, receipts, user!.Id);
            TempData[ok ? "Success" : "Error"] = ok ? "Delivery confirmed and inventory updated." : "Cannot confirm — already posted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var ok = await _deliveries.CancelAsync(id, reason);
            TempData[ok ? "Success" : "Error"] = ok ? "Delivery cancelled." : "Cannot cancel — inventory already posted.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
