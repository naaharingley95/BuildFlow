using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public class SupplierMaterial
    {
        [Key]
        public int Id { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; } = null!;

        public int MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal QuotedPrice { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }
    }
}
