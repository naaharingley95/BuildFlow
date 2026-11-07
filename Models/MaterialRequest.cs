using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public enum RequestStatus { Draft, Submitted, Approved, Rejected, Ordered, PartiallyFulfilled, Fulfilled }
    public enum RequestPriority { Low, Normal, High, Critical }

    public class MaterialRequest
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RequestNumber { get; set; } = string.Empty;

        public int SiteId { get; set; }

        [ForeignKey(nameof(SiteId))]
        public virtual ConstructionSite Site { get; set; } = null!;

        public string RequestedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(RequestedByUserId))]
        public virtual ApplicationUser RequestedBy { get; set; } = null!;

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime RequiredByDate { get; set; }
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        public RequestStatus Status { get; set; } = RequestStatus.Draft;

        [MaxLength(500)]
        public string? ReasonForRequest { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        // Navigation
        public virtual ICollection<MaterialRequestItem> Items { get; set; } = new List<MaterialRequestItem>();
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
