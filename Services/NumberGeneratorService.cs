using BuildFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Services
{
    public interface INumberGeneratorService
    {
        Task<string> GenerateRequestNumberAsync();
        Task<string> GeneratePONumberAsync();
        Task<string> GenerateDeliveryNumberAsync();
    }

    public class NumberGeneratorService : INumberGeneratorService
    {
        private readonly ApplicationDbContext _db;

        public NumberGeneratorService(ApplicationDbContext db) => _db = db;

        public async Task<string> GenerateRequestNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _db.MaterialRequests.CountAsync(r => r.RequestDate.Year == year);
            return $"MR-{year}-{(count + 1):D4}";
        }

        public async Task<string> GeneratePONumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _db.PurchaseOrders.CountAsync(p => p.OrderDate.Year == year);
            return $"PO-{year}-{(count + 1):D4}";
        }

        public async Task<string> GenerateDeliveryNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _db.Deliveries.CountAsync(d => d.ExpectedArrivalDate.Year == year);
            return $"DLV-{year}-{(count + 1):D4}";
        }
    }
}
