using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public class PurchaseOrderItem
    {
        [Key]
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; } = null!;

        [Column(TypeName = "decimal(18,4)")]
        public decimal OrderedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReceivedQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal SubTotal => OrderedQuantity * UnitPrice;

        [NotMapped]
        public decimal OutstandingQuantity => OrderedQuantity - ReceivedQuantity;

        [MaxLength(200)]
        public string? Remarks { get; set; }
    }
}
