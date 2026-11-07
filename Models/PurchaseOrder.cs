using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public enum PurchaseOrderStatus { Draft, Issued, PartiallyReceived, FullyReceived, Cancelled }

    public class PurchaseOrder
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public int? MaterialRequestId { get; set; }

        [ForeignKey(nameof(MaterialRequestId))]
        public virtual MaterialRequest? MaterialRequest { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; } = null!;

        public int SiteId { get; set; }

        [ForeignKey(nameof(SiteId))]
        public virtual ConstructionSite Site { get; set; } = null!;

        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedByUserId))]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpectedDeliveryDate { get; set; }
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherCharges { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(200)]
        public string? CancelReason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Computed
        [NotMapped]
        public decimal SubTotal => Items?.Sum(i => i.SubTotal) ?? 0;

        [NotMapped]
        public decimal TotalAmount => SubTotal + TaxAmount + OtherCharges;

        // Navigation
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    }
}
