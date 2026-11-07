using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface IPurchaseOrderService
    {
        Task<List<PurchaseOrder>> GetAllAsync(int? siteId, PurchaseOrderStatus? status);
        Task<PurchaseOrder?> GetByIdAsync(int id);
        Task<PurchaseOrder> CreateAsync(PurchaseOrder order, List<PurchaseOrderItem> items);
        Task<bool> IssueAsync(int id);
        Task<bool> CancelAsync(int id, string reason);
        Task UpdateReceivedQuantitiesAsync(int id);
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly INumberGeneratorService _numGen;

        public PurchaseOrderService(ApplicationDbContext db, INumberGeneratorService numGen)
        {
            _db = db;
            _numGen = numGen;
        }

        public async Task<List<PurchaseOrder>> GetAllAsync(int? siteId, PurchaseOrderStatus? status)
        {
            var q = _db.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Site)
                .Include(po => po.CreatedBy)
                .Include(po => po.Items)
                .AsQueryable();
            if (siteId.HasValue) q = q.Where(po => po.SiteId == siteId);
            if (status.HasValue) q = q.Where(po => po.Status == status);
            return await q.OrderByDescending(po => po.OrderDate).ToListAsync();
        }

        public async Task<PurchaseOrder?> GetByIdAsync(int id) =>
            await _db.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Site)
                .Include(po => po.CreatedBy)
                .Include(po => po.MaterialRequest)
                .Include(po => po.Items).ThenInclude(i => i.Material)
                .Include(po => po.Deliveries)
                .FirstOrDefaultAsync(po => po.Id == id);

        public async Task<PurchaseOrder> CreateAsync(PurchaseOrder order, List<PurchaseOrderItem> items)
        {
            // Validate — cannot create PO against rejected request
            if (order.MaterialRequestId.HasValue)
            {
                var req = await _db.MaterialRequests.FindAsync(order.MaterialRequestId.Value);
                if (req?.Status == RequestStatus.Rejected)
                    throw new InvalidOperationException("Cannot create purchase order against a rejected request.");
            }

            order.OrderNumber = await _numGen.GeneratePONumberAsync();
            order.CreatedAtUtc = DateTime.UtcNow;
            order.Status = PurchaseOrderStatus.Draft;
            _db.PurchaseOrders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in items)
            {
                item.PurchaseOrderId = order.Id;
                _db.PurchaseOrderItems.Add(item);
            }

            // Update material request status to Ordered
            if (order.MaterialRequestId.HasValue)
            {
                var req = await _db.MaterialRequests.FindAsync(order.MaterialRequestId.Value);
                if (req != null) req.Status = RequestStatus.Ordered;
            }

            await _db.SaveChangesAsync();
            return order;
        }

        public async Task<bool> IssueAsync(int id)
        {
            var po = await _db.PurchaseOrders.FindAsync(id);
            if (po == null || po.Status != PurchaseOrderStatus.Draft) return false;
            po.Status = PurchaseOrderStatus.Issued;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int id, string reason)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Deliveries)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (po == null) return false;
            if (po.Deliveries.Any(d => d.InventoryPosted))
                throw new InvalidOperationException("Cannot cancel — inventory already posted. Create an adjustment instead.");

            po.Status = PurchaseOrderStatus.Cancelled;
            po.CancelReason = reason;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task UpdateReceivedQuantitiesAsync(int id)
        {
            var po = await _db.PurchaseOrders
                .Include(po => po.Items)
                .FirstOrDefaultAsync(po => po.Id == id);
            if (po == null) return;

            foreach (var item in po.Items)
            {
                item.ReceivedQuantity = await _db.DeliveryItems
                    .Where(di => di.PurchaseOrderItemId == item.Id && di.Delivery.InventoryPosted)
                    .SumAsync(di => di.ReceivedQuantity);
            }

            var allFulfilled = po.Items.All(i => i.ReceivedQuantity >= i.OrderedQuantity);
            var anyFulfilled = po.Items.Any(i => i.ReceivedQuantity > 0);

            po.Status = allFulfilled ? PurchaseOrderStatus.FullyReceived
                      : anyFulfilled ? PurchaseOrderStatus.PartiallyReceived
                      : po.Status;

            await _db.SaveChangesAsync();
        }
    }
}
