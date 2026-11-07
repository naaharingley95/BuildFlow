using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface IMaterialRequestService
    {
        Task<List<MaterialRequest>> GetAllAsync(int? siteId, RequestStatus? status);
        Task<MaterialRequest?> GetByIdAsync(int id);
        Task<MaterialRequest> CreateAsync(MaterialRequest request, List<MaterialRequestItem> items);
        Task<bool> SubmitAsync(int id, string userId);
        Task<bool> ApproveAsync(int id, string userId, List<(int ItemId, decimal ApprovedQty)> approvals, string? remarks);
        Task<bool> RejectAsync(int id, string userId, string reason);
        Task<List<ConstructionSite>> GetAccessibleSitesAsync(string userId, bool isAdmin);
    }

    public class MaterialRequestService : IMaterialRequestService
    {
        private readonly ApplicationDbContext _db;
        private readonly INumberGeneratorService _numGen;

        public MaterialRequestService(ApplicationDbContext db, INumberGeneratorService numGen)
        {
            _db = db;
            _numGen = numGen;
        }

        public async Task<List<MaterialRequest>> GetAllAsync(int? siteId, RequestStatus? status)
        {
            var q = _db.MaterialRequests
                .Include(r => r.Site)
                .Include(r => r.RequestedBy)
                .AsQueryable();
            if (siteId.HasValue) q = q.Where(r => r.SiteId == siteId);
            if (status.HasValue) q = q.Where(r => r.Status == status);
            return await q.OrderByDescending(r => r.RequestDate).ToListAsync();
        }

        public async Task<MaterialRequest?> GetByIdAsync(int id) =>
            await _db.MaterialRequests
                .Include(r => r.Site)
                .Include(r => r.RequestedBy)
                .Include(r => r.Items).ThenInclude(i => i.Material)
                .Include(r => r.ApprovalHistories).ThenInclude(a => a.ActionByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<MaterialRequest> CreateAsync(MaterialRequest request, List<MaterialRequestItem> items)
        {
            request.RequestNumber = await _numGen.GenerateRequestNumberAsync();
            request.RequestDate = DateTime.UtcNow;
            request.Status = RequestStatus.Draft;
            _db.MaterialRequests.Add(request);
            await _db.SaveChangesAsync();

            foreach (var item in items)
            {
                item.MaterialRequestId = request.Id;
                _db.MaterialRequestItems.Add(item);
            }
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task<bool> SubmitAsync(int id, string userId)
        {
            var req = await _db.MaterialRequests.FindAsync(id);
            if (req == null || req.RequestedByUserId != userId || req.Status != RequestStatus.Draft)
                return false;

            req.Status = RequestStatus.Submitted;
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                MaterialRequestId = id, ActionByUserId = userId, Action = "Submitted",
                FromStatus = RequestStatus.Draft, ToStatus = RequestStatus.Submitted
            });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveAsync(int id, string userId,
            List<(int ItemId, decimal ApprovedQty)> approvals, string? remarks)
        {
            var req = await _db.MaterialRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
            if (req == null || req.Status != RequestStatus.Submitted) return false;

            foreach (var (itemId, qty) in approvals)
            {
                var item = req.Items.FirstOrDefault(i => i.Id == itemId);
                if (item != null) item.ApprovedQuantity = qty;
            }

            req.Status = RequestStatus.Approved;
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                MaterialRequestId = id, ActionByUserId = userId, Action = "Approved",
                FromStatus = RequestStatus.Submitted, ToStatus = RequestStatus.Approved,
                Remarks = remarks
            });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int id, string userId, string reason)
        {
            var req = await _db.MaterialRequests.FindAsync(id);
            if (req == null || req.Status != RequestStatus.Submitted) return false;

            req.Status = RequestStatus.Rejected;
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                MaterialRequestId = id, ActionByUserId = userId, Action = "Rejected",
                FromStatus = RequestStatus.Submitted, ToStatus = RequestStatus.Rejected,
                Remarks = reason
            });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<ConstructionSite>> GetAccessibleSitesAsync(string userId, bool isAdmin)
        {
            if (isAdmin)
                return await _db.ConstructionSites.Where(s => s.IsActive).ToListAsync();
            return await _db.ConstructionSites.Where(s => s.IsActive && s.SiteManagerId == userId).ToListAsync();
        }
    }
}
