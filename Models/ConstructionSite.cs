using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public enum SiteStatus { Planning, Active, OnHold, Completed, Cancelled }

    public class ConstructionSite
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string SiteName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ProjectDescription { get; set; }

        [Required, MaxLength(300)]
        public string Location { get; set; } = string.Empty;

        public string? SiteManagerId { get; set; }

        [ForeignKey(nameof(SiteManagerId))]
        public virtual ApplicationUser? SiteManager { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public SiteStatus Status { get; set; } = SiteStatus.Planning;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<SiteInventory> SiteInventories { get; set; } = new List<SiteInventory>();
        public virtual ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
        public virtual ICollection<MaterialConsumption> MaterialConsumptions { get; set; } = new List<MaterialConsumption>();
    }
}
