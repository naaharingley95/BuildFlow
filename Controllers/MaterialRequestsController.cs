using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class MaterialRequestsController : Controller
    {
        private readonly IMaterialRequestService _requests;
        private readonly IMaterialService _materials;
        private readonly UserManager<ApplicationUser> _userManager;

        public MaterialRequestsController(IMaterialRequestService requests, IMaterialService materials,
            UserManager<ApplicationUser> userManager)
        {
            _requests = requests;
            _materials = materials;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? siteId, RequestStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Procurement");
            ViewBag.Sites = await _requests.GetAccessibleSitesAsync(user!.Id, isAdmin);
            ViewBag.SiteId = siteId;
            ViewBag.Status = status;
            return View(await _requests.GetAllAsync(siteId, status));
        }

        public async Task<IActionResult> Details(int id)
        {
            var req = await _requests.GetByIdAsync(id);
            if (req == null) return NotFound();
            return View(req);
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            ViewBag.Sites = await _requests.GetAccessibleSitesAsync(user!.Id, isAdmin);
            ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
            return View(new MaterialRequest { RequestDate = DateTime.Today, RequiredByDate = DateTime.Today.AddDays(7) });
        }

        [HttpPost]
        public async Task<IActionResult> Create(MaterialRequest model, int[] materialIds, decimal[] quantities, string[] purposes)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            model.RequestedByUserId = user!.Id;

            var items = new List<MaterialRequestItem>();
            for (int i = 0; i < materialIds.Length; i++)
            {
                if (materialIds[i] > 0 && quantities[i] > 0)
                {
                    items.Add(new MaterialRequestItem
                    {
                        MaterialId = materialIds[i],
                        RequestedQuantity = quantities[i],
                        Purpose = i < purposes.Length ? purposes[i] : string.Empty
                    });
                }
            }

            if (items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "At least one item is required.");
                ViewBag.Sites = await _requests.GetAccessibleSitesAsync(user.Id, isAdmin);
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }

            ModelState.Remove(nameof(MaterialRequest.Site));
            ModelState.Remove(nameof(MaterialRequest.RequestedBy));
            ModelState.Remove(nameof(MaterialRequest.Items));
            ModelState.Remove(nameof(MaterialRequest.ApprovalHistories));

            if (!ModelState.IsValid)
            {
                ViewBag.Sites = await _requests.GetAccessibleSitesAsync(user.Id, isAdmin);
                ViewBag.Materials = await _materials.GetAllAsync(null, null, true);
                return View(model);
            }

            var created = await _requests.CreateAsync(model, items);
            TempData["Success"] = $"Request {created.RequestNumber} created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var ok = await _requests.SubmitAsync(id, user!.Id);
            TempData[ok ? "Success" : "Error"] = ok ? "Request submitted for approval." : "Cannot submit.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id, int[] itemIds, decimal[] approvedQtys, string? remarks)
        {
            var user = await _userManager.GetUserAsync(User);
            var approvals = itemIds.Zip(approvedQtys, (itemId, qty) => (itemId, qty)).ToList();
            var ok = await _requests.ApproveAsync(id, user!.Id, approvals, remarks);
            TempData[ok ? "Success" : "Error"] = ok ? "Request approved." : "Cannot approve.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Procurement")]
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var ok = await _requests.RejectAsync(id, user!.Id, reason);
            TempData[ok ? "Success" : "Error"] = ok ? "Request rejected." : "Cannot reject.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
