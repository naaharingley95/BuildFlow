using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildFlow.Models
{
    public class ApprovalHistory
    {
        [Key]
        public int Id { get; set; }

        public int MaterialRequestId { get; set; }

        [ForeignKey(nameof(MaterialRequestId))]
        public virtual MaterialRequest MaterialRequest { get; set; } = null!;

        public string ActionByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ActionByUserId))]
        public virtual ApplicationUser ActionByUser { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Submitted, Approved, Rejected, etc.

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime ActionDateUtc { get; set; } = DateTime.UtcNow;

        public RequestStatus FromStatus { get; set; }
        public RequestStatus ToStatus { get; set; }
    }
}
