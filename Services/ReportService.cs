using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    // ── DTOs ─────────────────────────────────────────────────────────────────────
    public record SiteInventoryReportRow(
        string SiteName, string Category, string MaterialName,
        string Unit, decimal Received, decimal Consumed, decimal Adjusted, decimal Available, decimal MinStock, bool IsLowStock);

    public record ConsumptionReportRow(
        string SiteName, string MaterialName, string Unit,
        string WorkActivity, decimal ConsumedQty, decimal WastageQty, DateTime Date, string RecordedBy);

    public record ProcurementReportRow(
        string OrderNumber, string SiteName, string SupplierName,
        DateTime OrderDate, decimal TotalAmount, PurchaseOrderStatus Status);

    public record MaterialRequestReportRow(
        string RequestNumber, string SiteName, string RequestedBy,
        DateTime RequestDate, RequestStatus Status, RequestPriority Priority, int ItemCount);

    // ── Interface ─────────────────────────────────────────────────────────────────
    public interface IReportService
    {
        Task<List<SiteInventoryReportRow>> GetInventoryReportAsync(int? siteId);
        Task<List<ConsumptionReportRow>> GetConsumptionReportAsync(int? siteId, DateTime? from, DateTime? to);
        Task<List<ProcurementReportRow>> GetProcurementReportAsync(int? siteId, DateTime? from, DateTime? to);
        Task<List<MaterialRequestReportRow>> GetRequestReportAsync(int? siteId, DateTime? from, DateTime? to);
    }

    // ── Implementation ────────────────────────────────────────────────────────────
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db) => _db = db;

        public async Task<List<SiteInventoryReportRow>> GetInventoryReportAsync(int? siteId)
        {
            var q = _db.SiteInventories
                .Include(si => si.Site)
                .Include(si => si.Material).ThenInclude(m => m.Category)
                .AsQueryable();

            if (siteId.HasValue) q = q.Where(si => si.SiteId == siteId);

            var data = await q.OrderBy(si => si.Site.SiteName).ThenBy(si => si.Material.Name).ToListAsync();

            return data.Select(si => new SiteInventoryReportRow(
                si.Site.SiteName,
                si.Material.Category?.Name ?? "Uncategorised",
                si.Material.Name,
                si.Material.Unit.ToString(),
                si.ReceivedQuantity,
                si.ConsumedQuantity,
                si.AdjustedQuantity,
                si.AvailableStock,
                si.MinimumStockLevel,
                si.IsLowStock)).ToList();
        }

        public async Task<List<ConsumptionReportRow>> GetConsumptionReportAsync(int? siteId, DateTime? from, DateTime? to)
        {
            var q = _db.MaterialConsumptions
                .Include(c => c.Site)
                .Include(c => c.Material)
                .Include(c => c.RecordedBy)
                .AsQueryable();

            if (siteId.HasValue) q = q.Where(c => c.SiteId == siteId);
            if (from.HasValue)   q = q.Where(c => c.ConsumptionDate >= from.Value);
            if (to.HasValue)     q = q.Where(c => c.ConsumptionDate <= to.Value);

            var data = await q.OrderByDescending(c => c.ConsumptionDate).ToListAsync();

            return data.Select(c => new ConsumptionReportRow(
                c.Site.SiteName,
                c.Material.Name,
                c.Material.Unit.ToString(),
                c.WorkActivity,
                c.ConsumedQuantity,
                c.WastageQuantity,
                c.ConsumptionDate,
                c.RecordedBy.FirstName + " " + c.RecordedBy.LastName)).ToList();
        }

        public async Task<List<ProcurementReportRow>> GetProcurementReportAsync(int? siteId, DateTime? from, DateTime? to)
        {
            var q = _db.PurchaseOrders
                .Include(po => po.Site)
                .Include(po => po.Supplier)
                .Include(po => po.Items)
                .AsQueryable();

            if (siteId.HasValue) q = q.Where(po => po.SiteId == siteId);
            if (from.HasValue)   q = q.Where(po => po.OrderDate >= from.Value);
            if (to.HasValue)     q = q.Where(po => po.OrderDate <= to.Value);

            var data = await q.OrderByDescending(po => po.OrderDate).ToListAsync();

            return data.Select(po => new ProcurementReportRow(
                po.OrderNumber,
                po.Site.SiteName,
                po.Supplier.CompanyName,
                po.OrderDate,
                po.TotalAmount,
                po.Status)).ToList();
        }

        public async Task<List<MaterialRequestReportRow>> GetRequestReportAsync(int? siteId, DateTime? from, DateTime? to)
        {
            var q = _db.MaterialRequests
                .Include(r => r.Site)
                .Include(r => r.RequestedBy)
                .Include(r => r.Items)
                .AsQueryable();

            if (siteId.HasValue) q = q.Where(r => r.SiteId == siteId);
            if (from.HasValue)   q = q.Where(r => r.RequestDate >= from.Value);
            if (to.HasValue)     q = q.Where(r => r.RequestDate <= to.Value);

            var data = await q.OrderByDescending(r => r.RequestDate).ToListAsync();

            return data.Select(r => new MaterialRequestReportRow(
                r.RequestNumber,
                r.Site.SiteName,
                r.RequestedBy.FirstName + " " + r.RequestedBy.LastName,
                r.RequestDate,
                r.Status,
                r.Priority,
                r.Items.Count)).ToList();
        }
    }
}
