using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public enum DeliveryStatus { Scheduled, Dispatched, PartiallyReceived, FullyReceived, Cancelled }

    public class Delivery
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string DeliveryNumber { get; set; } = string.Empty;

        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; } = null!;

        public int SiteId { get; set; }

        [ForeignKey(nameof(SiteId))]
        public virtual ConstructionSite Site { get; set; } = null!;

        public DateTime? DispatchDate { get; set; }
        public DateTime ExpectedArrivalDate { get; set; }
        public DateTime? ActualArrivalDate { get; set; }

        [MaxLength(50)]
        public string? VehicleNumber { get; set; }

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Scheduled;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool InventoryPosted { get; set; } = false;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<DeliveryItem> Items { get; set; } = new List<DeliveryItem>();
    }
}
