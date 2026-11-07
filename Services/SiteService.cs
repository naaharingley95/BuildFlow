using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface ISiteService
    {
        Task<List<ConstructionSite>> GetAllAsync(bool activeOnly = true);
        Task<ConstructionSite?> GetByIdAsync(int id);
        Task<ConstructionSite> CreateAsync(ConstructionSite site);
        Task UpdateAsync(ConstructionSite site);
        Task DeactivateAsync(int id);
        Task<SiteDetailsDto> GetSiteDetailsAsync(int id);
    }

    public record SiteDetailsDto(
        ConstructionSite Site,
        List<SiteInventory> Inventories,
        List<MaterialRequest> RecentRequests,
        List<PurchaseOrder> RecentOrders,
        List<Delivery> RecentDeliveries);

    public class SiteService : ISiteService
    {
        private readonly ApplicationDbContext _db;
        public SiteService(ApplicationDbContext db) => _db = db;

        public async Task<List<ConstructionSite>> GetAllAsync(bool activeOnly = true)
        {
            var q = _db.ConstructionSites.Include(s => s.SiteManager).AsQueryable();
            if (activeOnly) q = q.Where(s => s.IsActive);
            return await q.OrderBy(s => s.SiteName).ToListAsync();
        }

        public async Task DeactivateAsync(int id)
        {
            var site = await _db.ConstructionSites.FindAsync(id);
            if (site != null) { site.IsActive = false; await _db.SaveChangesAsync(); }
        }

        public async Task<ConstructionSite?> GetByIdAsync(int id) =>
            await _db.ConstructionSites.Include(s => s.SiteManager).FirstOrDefaultAsync(s => s.Id == id);

        public async Task<ConstructionSite> CreateAsync(ConstructionSite site)
        {
            site.CreatedAtUtc = DateTime.UtcNow;
            _db.ConstructionSites.Add(site);
            await _db.SaveChangesAsync();
            return site;
        }

        public async Task UpdateAsync(ConstructionSite site)
        {
            _db.ConstructionSites.Update(site);
            await _db.SaveChangesAsync();
        }

        public async Task<SiteDetailsDto> GetSiteDetailsAsync(int id)
        {
            var site = await _db.ConstructionSites.Include(s => s.SiteManager).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException();

            var inventories = await _db.SiteInventories
                .Include(si => si.Material).ThenInclude(m => m.Category)
                .Where(si => si.SiteId == id)
                .ToListAsync();

            var recentRequests = await _db.MaterialRequests
                .Include(r => r.RequestedBy)
                .Where(r => r.SiteId == id)
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToListAsync();

            var recentOrders = await _db.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.SiteId == id)
                .OrderByDescending(po => po.OrderDate)
                .Take(5)
                .ToListAsync();

            var recentDeliveries = await _db.Deliveries
                .Include(d => d.Supplier)
                .Where(d => d.SiteId == id)
                .OrderByDescending(d => d.ExpectedArrivalDate)
                .Take(5)
                .ToListAsync();

            return new SiteDetailsDto(site, inventories, recentRequests, recentOrders, recentDeliveries);
        }
    }
}
