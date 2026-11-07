using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface ISupplierService
    {
        Task<List<Supplier>> GetAllAsync(string? search, SupplierStatus? status);
        Task<Supplier?> GetByIdAsync(int id);
        Task<Supplier> CreateAsync(Supplier supplier);
        Task UpdateAsync(Supplier supplier);
        Task<SupplierDetailsDto> GetDetailsAsync(int id);
    }

    public record SupplierDetailsDto(Supplier Supplier, int TotalOrders, int CompletedOrders, int OutstandingOrders, decimal TotalSpend);

    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _db;
        public SupplierService(ApplicationDbContext db) => _db = db;

        public async Task<List<Supplier>> GetAllAsync(string? search, SupplierStatus? status)
        {
            var q = _db.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(s => s.CompanyName.Contains(search) || s.ContactPerson.Contains(search) || s.Email.Contains(search));
            if (status.HasValue)
                q = q.Where(s => s.Status == status);
            return await q.OrderBy(s => s.CompanyName).ToListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(int id) =>
            await _db.Suppliers.Include(s => s.SupplierMaterials).ThenInclude(sm => sm.Material)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            supplier.CreatedAtUtc = DateTime.UtcNow;
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();
            return supplier;
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            _db.Suppliers.Update(supplier);
            await _db.SaveChangesAsync();
        }

        public async Task<SupplierDetailsDto> GetDetailsAsync(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id) ?? throw new KeyNotFoundException();
            var orders = await _db.PurchaseOrders.Where(po => po.SupplierId == id).ToListAsync();
            var totalSpend = await _db.PurchaseOrderItems
                .Where(i => i.PurchaseOrder.SupplierId == id && i.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled)
                .SumAsync(i => i.OrderedQuantity * i.UnitPrice);

            return new SupplierDetailsDto(
                supplier,
                orders.Count,
                orders.Count(o => o.Status == PurchaseOrderStatus.FullyReceived),
                orders.Count(o => o.Status == PurchaseOrderStatus.Issued || o.Status == PurchaseOrderStatus.PartiallyReceived),
                totalSpend);
        }
    }
}
