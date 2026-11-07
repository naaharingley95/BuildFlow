using BuildFlow.Data;
using BuildFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;

        public DashboardService(ApplicationDbContext db) => _db = db;

        public async Task<DashboardSummary> GetDashboardSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var twelveMonthsAgo = now.AddMonths(-12);

            var summary = new DashboardSummary
            {
                TotalSites = await _db.ConstructionSites.CountAsync(s => s.IsActive),
                TotalMaterials = await _db.Materials.CountAsync(m => m.IsActive),
                ActiveSuppliers = await _db.Suppliers.CountAsync(s => s.Status == SupplierStatus.Active),
                PendingRequests = await _db.MaterialRequests.CountAsync(r =>
                    r.Status == RequestStatus.Submitted),
                ActivePurchaseOrders = await _db.PurchaseOrders.CountAsync(po =>
                    po.Status == PurchaseOrderStatus.Issued || po.Status == PurchaseOrderStatus.PartiallyReceived),
                PendingDeliveries = await _db.Deliveries.CountAsync(d =>
                    d.Status == DeliveryStatus.Scheduled || d.Status == DeliveryStatus.Dispatched),
                TotalProcurementExpenditure = await _db.PurchaseOrderItems
                    .Where(i => i.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled)
                    .SumAsync(i => i.OrderedQuantity * i.UnitPrice)
            };

            // Low stock
            var inventories = await _db.SiteInventories
                .Include(si => si.Site)
                .Include(si => si.Material)
                .ToListAsync();

            summary.LowStockItems = inventories.Count(si => si.IsLowStock);
            summary.LowStockAlerts = inventories
                .Where(si => si.IsLowStock)
                .OrderBy(si => si.AvailableStock)
                .Take(10)
                .Select(si => new LowStockAlert(
                    si.Site.SiteName,
                    si.Material.Name,
                    si.AvailableStock,
                    si.MinimumStockLevel,
                    si.Material.Unit.ToString()))
                .ToList();

            // Monthly expenditure (last 12 months)
            var poItems = await _db.PurchaseOrderItems
                .Where(i => i.PurchaseOrder.OrderDate >= twelveMonthsAgo && i.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled)
                .Select(i => new { i.PurchaseOrder.OrderDate, Amount = i.OrderedQuantity * i.UnitPrice })
                .ToListAsync();

            summary.MonthlyExpenditures = poItems
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyExpenditure(
                    $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yy}",
                    g.Sum(x => x.Amount)))
                .ToList();

            // Recent requests
            summary.RecentRequests = await _db.MaterialRequests
                .Include(r => r.Site)
                .Include(r => r.RequestedBy)
                .OrderByDescending(r => r.RequestDate)
                .Take(8)
                .Select(r => new RecentRequestDto(
                    r.Id, r.RequestNumber, r.Site.SiteName,
                    r.RequestedBy.FirstName + " " + r.RequestedBy.LastName,
                    r.RequestDate, r.Status, r.Priority))
                .ToListAsync();

            // Upcoming deliveries
            summary.UpcomingDeliveries = await _db.Deliveries
                .Include(d => d.Site)
                .Include(d => d.Supplier)
                .Where(d => d.Status == DeliveryStatus.Scheduled || d.Status == DeliveryStatus.Dispatched)
                .OrderBy(d => d.ExpectedArrivalDate)
                .Take(8)
                .Select(d => new UpcomingDeliveryDto(
                    d.Id, d.DeliveryNumber, d.Site.SiteName,
                    d.Supplier.CompanyName, d.ExpectedArrivalDate, d.Status))
                .ToListAsync();

            // PO status counts
            var poStatuses = await _db.PurchaseOrders
                .GroupBy(po => po.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            summary.PurchaseOrderStatusCounts = poStatuses.ToDictionary(x => x.Status.ToString(), x => x.Count);

            // Material consumption by category
            var consumptionData = await _db.MaterialConsumptions
                .Include(c => c.Material).ThenInclude(m => m.Category)
                .GroupBy(c => c.Material.Category.Name)
                .Select(g => new { Category = g.Key, Total = g.Sum(c => c.ConsumedQuantity) })
                .ToListAsync();
            summary.MaterialConsumptionByCategory = consumptionData.ToDictionary(x => x.Category, x => x.Total);

            return summary;
        }
    }
}
