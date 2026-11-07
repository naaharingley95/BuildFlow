using Microsoft.AspNetCore.Identity;

namespace BuildFlow.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginUtc { get; set; }

        // Navigation
        public virtual ICollection<ConstructionSite> ManagedSites { get; set; } = new List<ConstructionSite>();
        public virtual ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
    }
}
