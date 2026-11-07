using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface IDeliveryService
    {
        Task<List<Delivery>> GetAllAsync(int? siteId, DeliveryStatus? status);
        Task<Delivery?> GetByIdAsync(int id);
        Task<Delivery> CreateAsync(Delivery delivery, List<DeliveryItem> items);
        Task<bool> ConfirmReceiptAsync(int id, List<(int ItemId, decimal Received, decimal Rejected, string? Remarks)> receipts, string userId);
        Task<bool> CancelAsync(int id, string reason);
    }

    public class DeliveryService : IDeliveryService
    {
        private readonly ApplicationDbContext _db;
        private readonly INumberGeneratorService _numGen;
        private readonly IInventoryService _inventoryService;
        private readonly IPurchaseOrderService _poService;

        public DeliveryService(ApplicationDbContext db, INumberGeneratorService numGen,
            IInventoryService inventoryService, IPurchaseOrderService poService)
        {
            _db = db;
            _numGen = numGen;
            _inventoryService = inventoryService;
            _poService = poService;
        }

        public async Task<List<Delivery>> GetAllAsync(int? siteId, DeliveryStatus? status)
        {
            var q = _db.Deliveries
                .Include(d => d.Site).Include(d => d.Supplier).Include(d => d.PurchaseOrder)
                .AsQueryable();
            if (siteId.HasValue) q = q.Where(d => d.SiteId == siteId);
            if (status.HasValue) q = q.Where(d => d.Status == status);
            return await q.OrderByDescending(d => d.ExpectedArrivalDate).ToListAsync();
        }

        public async Task<Delivery?> GetByIdAsync(int id) =>
            await _db.Deliveries
                .Include(d => d.Site).Include(d => d.Supplier).Include(d => d.PurchaseOrder)
                .Include(d => d.Items).ThenInclude(i => i.Material)
                .Include(d => d.Items).ThenInclude(i => i.PurchaseOrderItem)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<Delivery> CreateAsync(Delivery delivery, List<DeliveryItem> items)
        {
            delivery.DeliveryNumber = await _numGen.GenerateDeliveryNumberAsync();
            delivery.CreatedAtUtc = DateTime.UtcNow;
            delivery.Status = DeliveryStatus.Scheduled;
            _db.Deliveries.Add(delivery);
            await _db.SaveChangesAsync();

            foreach (var item in items) { item.DeliveryId = delivery.Id; _db.DeliveryItems.Add(item); }
            await _db.SaveChangesAsync();
            return delivery;
        }

        public async Task<bool> ConfirmReceiptAsync(int id,
            List<(int ItemId, decimal Received, decimal Rejected, string? Remarks)> receipts,
            string userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var delivery = await _db.Deliveries
                    .Include(d => d.Items).ThenInclude(i => i.Material)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (delivery == null || delivery.InventoryPosted)
                    return false;

                foreach (var (itemId, received, rejected, remarks) in receipts)
                {
                    var item = delivery.Items.FirstOrDefault(i => i.Id == itemId);
                    if (item == null) continue;
                    item.ReceivedQuantity = received;
                    item.RejectedQuantity = rejected;
                    item.Remarks = remarks;

                    if (received > 0)
                    {
                        await _inventoryService.RecordReceiptAsync(
                            delivery.SiteId, item.MaterialId, received,
                            delivery.DeliveryNumber, userId);
                    }
                }

                var allReceived = delivery.Items.All(i => i.ReceivedQuantity + i.RejectedQuantity >= i.OrderedQuantity);
                delivery.Status = allReceived ? DeliveryStatus.FullyReceived : DeliveryStatus.PartiallyReceived;
                delivery.ActualArrivalDate = DateTime.UtcNow;
                delivery.InventoryPosted = true;

                await _db.SaveChangesAsync();
                await _poService.UpdateReceivedQuantitiesAsync(delivery.PurchaseOrderId);
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelAsync(int id, string reason)
        {
            var d = await _db.Deliveries.FindAsync(id);
            if (d == null || d.InventoryPosted) return false;
            d.Status = DeliveryStatus.Cancelled;
            d.Remarks = reason;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
