using BuildFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Data
{
    /// <summary>
    /// Seeds demonstration data. This is DEVELOPMENT ONLY — never run in production.
    /// </summary>
    public static class SeedData
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration config)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply any pending migrations
            await context.Database.MigrateAsync();

            // ── Roles ────────────────────────────────────────────────────────────────
            string[] roles = { "Administrator", "Admin", "ProcurementManager", "Procurement", "SiteManager", "InventoryManager", "StoreManager" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Admin user ───────────────────────────────────────────────────────────
            var adminEmail = config["SeedData:AdminEmail"] ?? "admin@buildflow.local";
            var adminPassword = config["SeedData:AdminPassword"] ?? "BuildFlow@Admin2024!";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrator");
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // ── Demo users ───────────────────────────────────────────────────────────
            async Task<ApplicationUser> EnsureUser(string email, string first, string last, params string[] userRoles)
            {
                var u = await userManager.FindByEmailAsync(email);
                if (u == null)
                {
                    u = new ApplicationUser
                    {
                        UserName = email, Email = email,
                        FirstName = first, LastName = last,
                        EmailConfirmed = true, IsActive = true
                    };
                    var r = await userManager.CreateAsync(u, "BuildFlow@Demo2024!");
                    if (r.Succeeded)
                    {
                        foreach (var role in userRoles)
                            await userManager.AddToRoleAsync(u, role);
                    }
                }
                return u!;
            }

            var pmUser = await EnsureUser("pm@buildflow.local", "Alice", "Procurement", "ProcurementManager", "Procurement");
            var sm1 = await EnsureUser("site1@buildflow.local", "Bob", "Builder", "SiteManager");
            var sm2 = await EnsureUser("site2@buildflow.local", "Carol", "Construction", "SiteManager");
            var invUser = await EnsureUser("inv@buildflow.local", "David", "Inventory", "InventoryManager", "StoreManager");

            if (await context.MaterialCategories.AnyAsync()) return; // Already seeded

            // ── Material Categories ──────────────────────────────────────────────────
            var catConcrete = new MaterialCategory { Name = "Concrete & Cement", Description = "Cement, concrete mixes and related products" };
            var catSteel    = new MaterialCategory { Name = "Steel & Metal",     Description = "Structural steel, reinforcement bars, sheets" };
            var catAggregate= new MaterialCategory { Name = "Aggregate",         Description = "Sand, gravel, crushed stone" };
            var catMasonry  = new MaterialCategory { Name = "Masonry",           Description = "Bricks, blocks, tiles" };
            var catPlumbing = new MaterialCategory { Name = "Plumbing",          Description = "Pipes, fittings, valves" };
            var catElectric = new MaterialCategory { Name = "Electrical",        Description = "Cables, conduits, switches" };
            var catFinish   = new MaterialCategory { Name = "Finishes",          Description = "Paint, plaster, adhesives" };
            context.MaterialCategories.AddRange(catConcrete, catSteel, catAggregate, catMasonry, catPlumbing, catElectric, catFinish);
            await context.SaveChangesAsync();

            // ── Materials ────────────────────────────────────────────────────────────
            var matCement = new Material { Name = "Ordinary Portland Cement", CategoryId = catConcrete.Id, Unit = UnitOfMeasurement.Bags, UnitPrice = 320, MinimumStockLevel = 50, Description = "50 kg bags, Grade 53" };
            var matRMC    = new Material { Name = "Ready-Mix Concrete M25",   CategoryId = catConcrete.Id, Unit = UnitOfMeasurement.CubicMetres, UnitPrice = 4800, MinimumStockLevel = 10 };
            var matTMT    = new Material { Name = "TMT Steel Bars Fe500",     CategoryId = catSteel.Id,    Unit = UnitOfMeasurement.Tonnes, UnitPrice = 68000, MinimumStockLevel = 5 };
            var matSand   = new Material { Name = "River Sand",               CategoryId = catAggregate.Id,Unit = UnitOfMeasurement.Tonnes, UnitPrice = 900,   MinimumStockLevel = 20 };
            var matAgg    = new Material { Name = "20mm Crushed Aggregate",   CategoryId = catAggregate.Id,Unit = UnitOfMeasurement.Tonnes, UnitPrice = 1100,  MinimumStockLevel = 20 };
            var matBricks = new Material { Name = "Red Clay Bricks",          CategoryId = catMasonry.Id,  Unit = UnitOfMeasurement.Pieces, UnitPrice = 8,     MinimumStockLevel = 5000 };
            var matPVC    = new Material { Name = "PVC Pipes 4-inch",         CategoryId = catPlumbing.Id, Unit = UnitOfMeasurement.Metres, UnitPrice = 180,   MinimumStockLevel = 50 };
            var matCable  = new Material { Name = "Electrical Cable 2.5 sqmm",CategoryId = catElectric.Id, Unit = UnitOfMeasurement.Metres, UnitPrice = 45,    MinimumStockLevel = 200 };
            var matPaint  = new Material { Name = "Exterior Emulsion Paint",  CategoryId = catFinish.Id,   Unit = UnitOfMeasurement.Litres, UnitPrice = 220,   MinimumStockLevel = 100 };
            var matTiles  = new Material { Name = "Floor Tiles 600x600mm",    CategoryId = catMasonry.Id,  Unit = UnitOfMeasurement.SquareMetres, UnitPrice = 650, MinimumStockLevel = 50 };
            context.Materials.AddRange(matCement, matRMC, matTMT, matSand, matAgg, matBricks, matPVC, matCable, matPaint, matTiles);
            await context.SaveChangesAsync();

            // ── Suppliers ────────────────────────────────────────────────────────────
            var sup1 = new Supplier { CompanyName = "BuildMart Supplies Ltd",   ContactPerson = "Rajan Mehta",     PhoneNumber = "+91-9876543210", Email = "rajan@buildmart.example", Address = "Industrial Area, Pune", MaterialCategoriesSupplied = "Concrete, Steel", PaymentTerms = PaymentTerms.Net30 };
            var sup2 = new Supplier { CompanyName = "SteelPro Industries",      ContactPerson = "Priya Sharma",    PhoneNumber = "+91-9823456789", Email = "priya@steelpro.example",  Address = "MIDC, Nagpur",          MaterialCategoriesSupplied = "Steel & Metal",   PaymentTerms = PaymentTerms.Net45 };
            var sup3 = new Supplier { CompanyName = "Aggregate World",           ContactPerson = "Suresh Patil",    PhoneNumber = "+91-9012345678", Email = "suresh@aggworld.example", Address = "Quarry Zone, Nashik",   MaterialCategoriesSupplied = "Aggregate",        PaymentTerms = PaymentTerms.CashOnDelivery };
            var sup4 = new Supplier { CompanyName = "ElectroFit Solutions",      ContactPerson = "Meena Desai",     PhoneNumber = "+91-9345678901", Email = "meena@electrofit.example",Address = "Electronic City, Pune", MaterialCategoriesSupplied = "Electrical",       PaymentTerms = PaymentTerms.Net15 };
            var sup5 = new Supplier { CompanyName = "ColorCoat Paints & Tiles",  ContactPerson = "Arjun Singh",     PhoneNumber = "+91-9456789012", Email = "arjun@colorcoat.example", Address = "Paint Nagar, Mumbai",   MaterialCategoriesSupplied = "Finishes, Masonry",PaymentTerms = PaymentTerms.Net30 };
            context.Suppliers.AddRange(sup1, sup2, sup3, sup4, sup5);
            await context.SaveChangesAsync();

            // ── Construction Sites ───────────────────────────────────────────────────
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            var site1 = new ConstructionSite { SiteName = "Skyline Towers Phase 1", ProjectDescription = "28-storey residential complex with 240 apartments", Location = "Baner, Pune", SiteManagerId = sm1.Id, StartDate = DateTime.Today.AddMonths(-6), ExpectedCompletionDate = DateTime.Today.AddMonths(18), Status = SiteStatus.Active };
            var site2 = new ConstructionSite { SiteName = "Metro Bridge Connector",  ProjectDescription = "Road overbridge connecting NH48 to ring road",        Location = "Hadapsar, Pune", SiteManagerId = sm2.Id, StartDate = DateTime.Today.AddMonths(-3), ExpectedCompletionDate = DateTime.Today.AddMonths(9),  Status = SiteStatus.Active };
            var site3 = new ConstructionSite { SiteName = "GreenPark Commercial Hub",ProjectDescription = "Mixed-use commercial complex — offices, retail, parking", Location = "Hinjewadi, Pune", SiteManagerId = sm1.Id, StartDate = DateTime.Today.AddMonths(1), ExpectedCompletionDate = DateTime.Today.AddMonths(24), Status = SiteStatus.Planning };
            context.ConstructionSites.AddRange(site1, site2, site3);
            await context.SaveChangesAsync();

            // ── Site Inventories (opening stock) ─────────────────────────────────────
            void AddInv(ConstructionSite site, Material mat, decimal open, decimal received, decimal consumed)
            {
                context.SiteInventories.Add(new SiteInventory
                {
                    SiteId = site.Id, MaterialId = mat.Id,
                    OpeningStock = open, ReceivedQuantity = received, ConsumedQuantity = consumed,
                    MinimumStockLevel = mat.MinimumStockLevel, LastUpdatedUtc = DateTime.UtcNow
                });
            }

            AddInv(site1, matCement, 200, 500, 480);
            AddInv(site1, matTMT,    10,   30,  25);
            AddInv(site1, matSand,   50,  100,  80);
            AddInv(site1, matBricks, 0, 20000, 12000);
            AddInv(site2, matCement, 100, 200,  190);
            AddInv(site2, matRMC,    0,   50,   48);
            AddInv(site2, matAgg,    30,  80,   60);
            AddInv(site2, matPVC,    0,  200,  100);
            await context.SaveChangesAsync();

            // ── Material Requests ────────────────────────────────────────────────────
            var req1 = new MaterialRequest
            {
                RequestNumber = "REQ-2024-0001", SiteId = site1.Id, RequestedByUserId = sm1.Id,
                RequiredByDate = DateTime.UtcNow.AddDays(7), Priority = RequestPriority.High,
                Status = RequestStatus.Approved, ReasonForRequest = "Foundation slab pour scheduled",
                RequestDate = DateTime.UtcNow.AddDays(-5)
            };
            var req2 = new MaterialRequest
            {
                RequestNumber = "REQ-2024-0002", SiteId = site2.Id, RequestedByUserId = sm2.Id,
                RequiredByDate = DateTime.UtcNow.AddDays(14), Priority = RequestPriority.Normal,
                Status = RequestStatus.Submitted, ReasonForRequest = "Plumbing rough-in for block B",
                RequestDate = DateTime.UtcNow.AddDays(-2)
            };
            var req3 = new MaterialRequest
            {
                RequestNumber = "REQ-2024-0003", SiteId = site1.Id, RequestedByUserId = sm1.Id,
                RequiredByDate = DateTime.UtcNow.AddDays(21), Priority = RequestPriority.Normal,
                Status = RequestStatus.Draft, ReasonForRequest = "Finishing works — paint & tiles",
                RequestDate = DateTime.UtcNow.AddDays(-1)
            };
            context.MaterialRequests.AddRange(req1, req2, req3);
            await context.SaveChangesAsync();

            context.MaterialRequestItems.AddRange(
                new MaterialRequestItem { MaterialRequestId = req1.Id, MaterialId = matCement.Id, RequestedQuantity = 200, ApprovedQuantity = 200 },
                new MaterialRequestItem { MaterialRequestId = req1.Id, MaterialId = matSand.Id,   RequestedQuantity = 50,  ApprovedQuantity = 50 },
                new MaterialRequestItem { MaterialRequestId = req2.Id, MaterialId = matPVC.Id,    RequestedQuantity = 300, ApprovedQuantity = 0 },
                new MaterialRequestItem { MaterialRequestId = req3.Id, MaterialId = matPaint.Id,  RequestedQuantity = 500, ApprovedQuantity = 0 },
                new MaterialRequestItem { MaterialRequestId = req3.Id, MaterialId = matTiles.Id,  RequestedQuantity = 200, ApprovedQuantity = 0 }
            );
            await context.SaveChangesAsync();

            // Approval history for req1
            context.ApprovalHistories.AddRange(
                new ApprovalHistory { MaterialRequestId = req1.Id, ActionByUserId = sm1.Id, Action = "Submitted", FromStatus = RequestStatus.Draft, ToStatus = RequestStatus.Submitted, ActionDateUtc = DateTime.UtcNow.AddDays(-5) },
                new ApprovalHistory { MaterialRequestId = req1.Id, ActionByUserId = pmUser.Id, Action = "Approved", FromStatus = RequestStatus.Submitted, ToStatus = RequestStatus.Approved, Remarks = "Approved for foundation slab", ActionDateUtc = DateTime.UtcNow.AddDays(-4) }
            );
            await context.SaveChangesAsync();

            // ── Purchase Orders ──────────────────────────────────────────────────────
            var adminId = adminUser!.Id;
            var po1 = new PurchaseOrder
            {
                OrderNumber = "PO-2024-0001", MaterialRequestId = req1.Id, SupplierId = sup1.Id, SiteId = site1.Id,
                CreatedByUserId = adminId, OrderDate = DateTime.UtcNow.AddDays(-4),
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(3), Status = PurchaseOrderStatus.Issued, TaxAmount = 5760, OtherCharges = 0,
                Notes = "Delivery at gate 2. Contact Bob Builder."
            };
            context.PurchaseOrders.Add(po1);
            await context.SaveChangesAsync();

            context.PurchaseOrderItems.AddRange(
                new PurchaseOrderItem { PurchaseOrderId = po1.Id, MaterialId = matCement.Id, OrderedQuantity = 200, UnitPrice = 320 },
                new PurchaseOrderItem { PurchaseOrderId = po1.Id, MaterialId = matSand.Id,   OrderedQuantity = 50,  UnitPrice = 900 }
            );
            await context.SaveChangesAsync();

            // ── Deliveries ───────────────────────────────────────────────────────────
            var poItems = await context.PurchaseOrderItems.Where(i => i.PurchaseOrderId == po1.Id).ToListAsync();
            var del1 = new Delivery
            {
                DeliveryNumber = "DEL-2024-0001", PurchaseOrderId = po1.Id, SupplierId = sup1.Id, SiteId = site1.Id,
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(3), DispatchDate = DateTime.UtcNow.AddDays(-1),
                VehicleNumber = "MH12-AB-1234", Status = DeliveryStatus.Dispatched
            };
            context.Deliveries.Add(del1);
            await context.SaveChangesAsync();

            context.DeliveryItems.AddRange(
                new DeliveryItem { DeliveryId = del1.Id, PurchaseOrderItemId = poItems[0].Id, MaterialId = matCement.Id, OrderedQuantity = 200, ReceivedQuantity = 0 },
                new DeliveryItem { DeliveryId = del1.Id, PurchaseOrderItemId = poItems[1].Id, MaterialId = matSand.Id,   OrderedQuantity = 50,  ReceivedQuantity = 0 }
            );
            await context.SaveChangesAsync();

            // ── Consumption records ──────────────────────────────────────────────────
            var siteInvCement1 = await context.SiteInventories.FirstAsync(si => si.SiteId == site1.Id && si.MaterialId == matCement.Id);
            context.MaterialConsumptions.AddRange(
                new MaterialConsumption { SiteId = site1.Id, MaterialId = matCement.Id, ConsumedQuantity = 100, WastageQuantity = 2, WorkActivity = "Column casting — Grid A", ConsumptionDate = DateTime.Today.AddDays(-10), RecordedByUserId = invUser.Id },
                new MaterialConsumption { SiteId = site1.Id, MaterialId = matCement.Id, ConsumedQuantity = 80,  WastageQuantity = 1, WorkActivity = "Beam casting — Level 3",  ConsumptionDate = DateTime.Today.AddDays(-5),  RecordedByUserId = invUser.Id },
                new MaterialConsumption { SiteId = site1.Id, MaterialId = matTMT.Id,    ConsumedQuantity = 5,   WastageQuantity = 0, WorkActivity = "Reinforcement — columns",   ConsumptionDate = DateTime.Today.AddDays(-8),  RecordedByUserId = invUser.Id }
            );
            await context.SaveChangesAsync();
        }
    }
}
