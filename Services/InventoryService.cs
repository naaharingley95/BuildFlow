using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface IInventoryService
    {
        Task<List<SiteInventory>> GetSiteInventoryAsync(int siteId);
        Task<List<SiteInventory>> GetLowStockAsync();
        Task<List<InventoryTransaction>> GetTransactionsAsync(int siteInventoryId);
        Task<SiteInventory> GetOrCreateInventoryRecordAsync(int siteId, int materialId);
        Task RecordReceiptAsync(int siteId, int materialId, decimal qty, string reference, string userId);
        Task RecordAdjustmentAsync(int siteId, int materialId, decimal qty, string reason, string userId);
    }

    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;

        public InventoryService(ApplicationDbContext db) => _db = db;

        public async Task<List<SiteInventory>> GetSiteInventoryAsync(int siteId) =>
            await _db.SiteInventories
                .Include(si => si.Material).ThenInclude(m => m.Category)
                .Where(si => si.SiteId == siteId)
                .ToListAsync();

        public async Task<List<SiteInventory>> GetLowStockAsync() =>
            await _db.SiteInventories
                .Include(si => si.Site)
                .Include(si => si.Material)
                .ToListAsync()
                .ContinueWith(t => t.Result.Where(si => si.IsLowStock).ToList());

        public async Task<List<InventoryTransaction>> GetTransactionsAsync(int siteInventoryId) =>
            await _db.InventoryTransactions
                .Include(t => t.PerformedBy)
                .Where(t => t.SiteInventoryId == siteInventoryId)
                .OrderByDescending(t => t.TransactionDateUtc)
                .ToListAsync();

        public async Task<SiteInventory> GetOrCreateInventoryRecordAsync(int siteId, int materialId)
        {
            var inv = await _db.SiteInventories
                .FirstOrDefaultAsync(si => si.SiteId == siteId && si.MaterialId == materialId);
            if (inv != null) return inv;

            var material = await _db.Materials.FindAsync(materialId);
            inv = new SiteInventory
            {
                SiteId = siteId, MaterialId = materialId,
                MinimumStockLevel = material?.MinimumStockLevel ?? 0,
                LastUpdatedUtc = DateTime.UtcNow
            };
            _db.SiteInventories.Add(inv);
            await _db.SaveChangesAsync();
            return inv;
        }

        public async Task RecordReceiptAsync(int siteId, int materialId, decimal qty, string reference, string userId)
        {
            var inv = await GetOrCreateInventoryRecordAsync(siteId, materialId);
            inv.ReceivedQuantity += qty;
            inv.LastUpdatedUtc = DateTime.UtcNow;

            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                SiteInventoryId = inv.Id, TransactionType = TransactionType.Received,
                Quantity = qty, Reference = reference, PerformedByUserId = userId,
                TransactionDateUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task RecordAdjustmentAsync(int siteId, int materialId, decimal qty, string reason, string userId)
        {
            var inv = await GetOrCreateInventoryRecordAsync(siteId, materialId);
            inv.AdjustedQuantity += qty;
            inv.LastUpdatedUtc = DateTime.UtcNow;

            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                SiteInventoryId = inv.Id, TransactionType = TransactionType.Adjustment,
                Quantity = qty, Reference = "ADJ", Remarks = reason, PerformedByUserId = userId,
                TransactionDateUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
