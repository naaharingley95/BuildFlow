using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public class MaterialConsumption
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
        public decimal ConsumedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal WastageQuantity { get; set; }

        [Required, MaxLength(300)]
        public string WorkActivity { get; set; } = string.Empty;

        public DateTime ConsumptionDate { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public string RecordedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(RecordedByUserId))]
        public virtual ApplicationUser RecordedBy { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
