using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public class DeliveryItem
    {
        [Key]
        public int Id { get; set; }

        public int DeliveryId { get; set; }

        [ForeignKey(nameof(DeliveryId))]
        public virtual Delivery Delivery { get; set; } = null!;

        public int PurchaseOrderItemId { get; set; }

        [ForeignKey(nameof(PurchaseOrderItemId))]
        public virtual PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;

        public int MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; } = null!;

        [Column(TypeName = "decimal(18,4)")]
        public decimal OrderedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReceivedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal RejectedQuantity { get; set; }

        [MaxLength(300)]
        public string? Remarks { get; set; }
    }
}
