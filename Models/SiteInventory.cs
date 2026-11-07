using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public class SiteInventory
    {
        [Key]
        public int Id { get; set; }

        public int SiteId { get; set; }

        [ForeignKey(nameof(SiteId))]
        public virtual ConstructionSite Site { get; set; } = null!;

        public int MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; } = null!;

        [Column(TypeName = "decimal(18,4)")]
        public decimal OpeningStock { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReceivedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ConsumedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReturnedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal DamagedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal AdjustedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal MinimumStockLevel { get; set; }

        [NotMapped]
        public decimal AvailableStock =>
            OpeningStock + ReceivedQuantity + ReturnedQuantity + AdjustedQuantity
            - ConsumedQuantity - DamagedQuantity;

        [NotMapped]
        public bool IsLowStock => AvailableStock <= MinimumStockLevel;

        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    }
}
