using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public enum TransactionType
    {
        OpeningStock, Received, Consumed, Returned, Damaged, Adjustment
    }

    public class InventoryTransaction
    {
        [Key]
        public int Id { get; set; }

        public int SiteInventoryId { get; set; }

        [ForeignKey(nameof(SiteInventoryId))]
        public virtual SiteInventory SiteInventory { get; set; } = null!;

        public TransactionType TransactionType { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }  // Positive = in, Negative = out for Adjustment

        [MaxLength(300)]
        public string? Reference { get; set; }  // Delivery number, consumption ID, etc.

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public string PerformedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(PerformedByUserId))]
        public virtual ApplicationUser PerformedBy { get; set; } = null!;

        public DateTime TransactionDateUtc { get; set; } = DateTime.UtcNow;
    }
}
