using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface IMaterialService
    {
        Task<List<Material>> GetAllAsync(string? search, int? categoryId, bool? isActive);
        Task<Material?> GetByIdAsync(int id);
        Task<Material> CreateAsync(Material material);
        Task UpdateAsync(Material material);
        Task<bool> DeleteAsync(int id);
        Task<List<MaterialCategory>> GetCategoriesAsync();
        Task<bool> IsReferencedAsync(int id);
    }

    public class MaterialService : IMaterialService
    {
        private readonly ApplicationDbContext _db;
        public MaterialService(ApplicationDbContext db) => _db = db;

        public async Task<List<Material>> GetAllAsync(string? search, int? categoryId, bool? isActive)
        {
            var q = _db.Materials.Include(m => m.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(m => m.Name.Contains(search) || m.Description!.Contains(search));
            if (categoryId.HasValue)
                q = q.Where(m => m.CategoryId == categoryId);
            if (isActive.HasValue)
                q = q.Where(m => m.IsActive == isActive);
            return await q.OrderBy(m => m.Name).ToListAsync();
        }

        public async Task<Material?> GetByIdAsync(int id) =>
            await _db.Materials.Include(m => m.Category).FirstOrDefaultAsync(m => m.Id == id);

        public async Task<Material> CreateAsync(Material material)
        {
            material.CreatedAtUtc = DateTime.UtcNow;
            _db.Materials.Add(material);
            await _db.SaveChangesAsync();
            return material;
        }

        public async Task UpdateAsync(Material material)
        {
            _db.Materials.Update(material);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (await IsReferencedAsync(id)) return false;
            var m = await _db.Materials.FindAsync(id);
            if (m == null) return false;
            _db.Materials.Remove(m);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsReferencedAsync(int id) =>
            await _db.MaterialRequestItems.AnyAsync(i => i.MaterialId == id) ||
            await _db.PurchaseOrderItems.AnyAsync(i => i.MaterialId == id) ||
            await _db.SiteInventories.AnyAsync(si => si.MaterialId == id);

        public async Task<List<MaterialCategory>> GetCategoriesAsync() =>
            await _db.MaterialCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
    }
}
