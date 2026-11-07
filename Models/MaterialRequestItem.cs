using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public class MaterialRequestItem
    {
        [Key]
        public int Id { get; set; }

        public int MaterialRequestId { get; set; }

        [ForeignKey(nameof(MaterialRequestId))]
        public virtual MaterialRequest MaterialRequest { get; set; } = null!;

        public int MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; } = null!;

        [Column(TypeName = "decimal(18,4)")]
        public decimal RequestedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ApprovedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal OrderedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal FulfilledQuantity { get; set; }

        [MaxLength(300)]
        public string? Purpose { get; set; }

        [MaxLength(300)]
        public string? Remarks { get; set; }
    }
}
