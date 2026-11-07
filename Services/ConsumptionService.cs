using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface IConsumptionService
    {
        Task<List<MaterialConsumption>> GetAllAsync(int? siteId, int? materialId, DateTime? from, DateTime? to);
        Task<MaterialConsumption?> GetByIdAsync(int id);
        Task<MaterialConsumption> RecordAsync(MaterialConsumption consumption, string userId);
        Task<List<MaterialConsumption>> GetBySiteAsync(int siteId, DateTime? from, DateTime? to);
    }

    public class ConsumptionService : IConsumptionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventory;

        public ConsumptionService(ApplicationDbContext db, IInventoryService inventory)
        {
            _db = db;
            _inventory = inventory;
        }

        public async Task<List<MaterialConsumption>> GetAllAsync(int? siteId, int? materialId, DateTime? from, DateTime? to)
        {
            var q = _db.MaterialConsumptions
                .Include(c => c.Site)
                .Include(c => c.Material).ThenInclude(m => m.Category)
                .Include(c => c.RecordedBy)
                .AsQueryable();

            if (siteId.HasValue)     q = q.Where(c => c.SiteId == siteId);
            if (materialId.HasValue) q = q.Where(c => c.MaterialId == materialId);
            if (from.HasValue)       q = q.Where(c => c.ConsumptionDate >= from.Value);
            if (to.HasValue)         q = q.Where(c => c.ConsumptionDate <= to.Value);

            return await q.OrderByDescending(c => c.ConsumptionDate).ToListAsync();
        }

        public async Task<MaterialConsumption?> GetByIdAsync(int id) =>
            await _db.MaterialConsumptions
                .Include(c => c.Site)
                .Include(c => c.Material).ThenInclude(m => m.Category)
                .Include(c => c.RecordedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<MaterialConsumption> RecordAsync(MaterialConsumption consumption, string userId)
        {
            consumption.RecordedByUserId = userId;
            consumption.CreatedAtUtc = DateTime.UtcNow;
            _db.MaterialConsumptions.Add(consumption);

            // Reduce inventory
            var totalQty = consumption.ConsumedQuantity + consumption.WastageQuantity;
            await _inventory.RecordAdjustmentAsync(
                consumption.SiteId,
                consumption.MaterialId,
                -totalQty,
                $"Consumed: {consumption.WorkActivity}",
                userId);

            await _db.SaveChangesAsync();
            return consumption;
        }

        public async Task<List<MaterialConsumption>> GetBySiteAsync(int siteId, DateTime? from, DateTime? to)
            => await GetAllAsync(siteId, null, from, to);
    }
}
