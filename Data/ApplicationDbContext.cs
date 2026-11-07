using BuildFlow.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ConstructionSite> ConstructionSites => Set<ConstructionSite>();
        public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SupplierMaterial> SupplierMaterials => Set<SupplierMaterial>();
        public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>();
        public DbSet<MaterialRequestItem> MaterialRequestItems => Set<MaterialRequestItem>();
        public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
        public DbSet<Delivery> Deliveries => Set<Delivery>();
        public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
        public DbSet<SiteInventory> SiteInventories => Set<SiteInventory>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<MaterialConsumption> MaterialConsumptions => Set<MaterialConsumption>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique composite index: one inventory record per site/material
            builder.Entity<SiteInventory>()
                .HasIndex(si => new { si.SiteId, si.MaterialId })
                .IsUnique();

            // Prevent EF cascade delete cycles involving ApplicationUser
            builder.Entity<ConstructionSite>()
                .HasOne(s => s.SiteManager)
                .WithMany(u => u.ManagedSites)
                .HasForeignKey(s => s.SiteManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<MaterialRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany(u => u.MaterialRequests)
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApprovalHistory>()
                .HasOne(a => a.ActionByUser)
                .WithMany(u => u.ApprovalHistories)
                .HasForeignKey(a => a.ActionByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrder>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryTransaction>()
                .HasOne(t => t.PerformedBy)
                .WithMany()
                .HasForeignKey(t => t.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaterialConsumption>()
                .HasOne(c => c.RecordedBy)
                .WithMany()
                .HasForeignKey(c => c.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supplier-material unique combination
            builder.Entity<SupplierMaterial>()
                .HasIndex(sm => new { sm.SupplierId, sm.MaterialId })
                .IsUnique();

            // Decimal precision for monetary values already set via Column attribute,
            // but ensure consistent indexing for performance
            builder.Entity<PurchaseOrder>()
                .HasIndex(po => po.OrderNumber)
                .IsUnique();

            builder.Entity<MaterialRequest>()
                .HasIndex(mr => mr.RequestNumber)
                .IsUnique();

            builder.Entity<Delivery>()
                .HasIndex(d => d.DeliveryNumber)
                .IsUnique();
        }
    }
}
