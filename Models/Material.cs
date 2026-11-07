using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public enum UnitOfMeasurement { Bags, Kilograms, Tonnes, Pieces, CubicMetres, Litres, Metres, SquareMetres, Rolls, Sets }

    public class Material
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual MaterialCategory Category { get; set; } = null!;

        public UnitOfMeasurement Unit { get; set; } = UnitOfMeasurement.Pieces;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal MinimumStockLevel { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<SiteInventory> SiteInventories { get; set; } = new List<SiteInventory>();
        public virtual ICollection<MaterialRequestItem> RequestItems { get; set; } = new List<MaterialRequestItem>();
        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
        public virtual ICollection<MaterialConsumption> MaterialConsumptions { get; set; } = new List<MaterialConsumption>();
        public virtual ICollection<SupplierMaterial> SupplierMaterials { get; set; } = new List<SupplierMaterial>();
    }
}
