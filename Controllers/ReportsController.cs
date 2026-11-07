using BuildFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reports;
        private readonly ISiteService _sites;

        public ReportsController(IReportService reports, ISiteService sites)
        {
            _reports = reports;
            _sites = sites;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> Inventory(int? siteId)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.SiteId = siteId;
            return View(await _reports.GetInventoryReportAsync(siteId));
        }

        public async Task<IActionResult> Consumption(int? siteId, DateTime? from, DateTime? to)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.SiteId = siteId;
            ViewBag.From = from;
            ViewBag.To = to;
            return View(await _reports.GetConsumptionReportAsync(siteId, from, to));
        }

        public async Task<IActionResult> Procurement(int? siteId, DateTime? from, DateTime? to)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.SiteId = siteId;
            ViewBag.From = from;
            ViewBag.To = to;
            return View(await _reports.GetProcurementReportAsync(siteId, from, to));
        }

        public async Task<IActionResult> Requests(int? siteId, DateTime? from, DateTime? to)
        {
            ViewBag.Sites = await _sites.GetAllAsync();
            ViewBag.SiteId = siteId;
            ViewBag.From = from;
            ViewBag.To = to;
            return View(await _reports.GetRequestReportAsync(siteId, from, to));
        }

        public async Task<IActionResult> ExportInventoryCsv(int? siteId)
        {
            var data = await _reports.GetInventoryReportAsync(siteId);
            return ToCsvFile(data, $"inventory-report-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> ExportConsumptionCsv(int? siteId, DateTime? from, DateTime? to)
        {
            var data = await _reports.GetConsumptionReportAsync(siteId, from, to);
            return ToCsvFile(data, $"consumption-report-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> ExportProcurementCsv(int? siteId, DateTime? from, DateTime? to)
        {
            var data = await _reports.GetProcurementReportAsync(siteId, from, to);
            return ToCsvFile(data, $"procurement-report-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> ExportRequestsCsv(int? siteId, DateTime? from, DateTime? to)
        {
            var data = await _reports.GetRequestReportAsync(siteId, from, to);
            return ToCsvFile(data, $"requests-report-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        private FileContentResult ToCsvFile<T>(IEnumerable<T> records, string fileName)
        {
            using var memoryStream = new System.IO.MemoryStream();
            using (var writer = new System.IO.StreamWriter(memoryStream, System.Text.Encoding.UTF8))
            using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
            return File(memoryStream.ToArray(), "text/csv", fileName);
        }
    }
}
