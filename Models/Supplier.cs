using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public enum PaymentTerms { Net7, Net15, Net30, Net45, Net60, CashOnDelivery, Advance }
    public enum SupplierStatus { Active, Inactive, Blacklisted }

    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MaxLength(200), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Address { get; set; }

        [MaxLength(300)]
        public string? MaterialCategoriesSupplied { get; set; }

        public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
        public SupplierStatus Status { get; set; } = SupplierStatus.Active;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<SupplierMaterial> SupplierMaterials { get; set; } = new List<SupplierMaterial>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    }
}
