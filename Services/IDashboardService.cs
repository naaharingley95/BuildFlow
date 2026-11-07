using BuildFlow.Models;

namespace BuildFlow.Services
{
    public class DashboardSummary
    {
        public int TotalSites { get; set; }
        public int TotalMaterials { get; set; }
        public int ActiveSuppliers { get; set; }
        public int PendingRequests { get; set; }
        public int ActivePurchaseOrders { get; set; }
        public int PendingDeliveries { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalProcurementExpenditure { get; set; }
        public List<MonthlyExpenditure> MonthlyExpenditures { get; set; } = new();
        public List<LowStockAlert> LowStockAlerts { get; set; } = new();
        public List<RecentRequestDto> RecentRequests { get; set; } = new();
        public List<UpcomingDeliveryDto> UpcomingDeliveries { get; set; } = new();
        public Dictionary<string, int> PurchaseOrderStatusCounts { get; set; } = new();
        public Dictionary<string, decimal> MaterialConsumptionByCategory { get; set; } = new();
    }

    public record MonthlyExpenditure(string Month, decimal Amount);
    public record LowStockAlert(string SiteName, string MaterialName, decimal Available, decimal Minimum, string Unit);
    public record RecentRequestDto(int Id, string RequestNumber, string SiteName, string RequestedBy, DateTime RequestDate, RequestStatus Status, RequestPriority Priority);
    public record UpcomingDeliveryDto(int Id, string DeliveryNumber, string SiteName, string SupplierName, DateTime ExpectedArrival, DeliveryStatus Status);

    public interface IDashboardService
    {
        Task<DashboardSummary> GetDashboardSummaryAsync();
    }
}
