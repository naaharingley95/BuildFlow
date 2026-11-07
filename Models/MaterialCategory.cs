using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Models
{
    public class MaterialCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
