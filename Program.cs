using BuildFlow.Data;
using BuildFlow.Models;
using BuildFlow.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // SQLite for all environments — zero-install, persistent file on disk.
    // On Railway: file is stored in /app/data/ (persistent volume mount).
    var connStr = builder.Configuration.GetConnectionString("SqliteConnection")
                  ?? "Data Source=buildflow.db";
    options.UseSqlite(connStr);
});

// ── Identity ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength         = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail         = true;
    options.SignIn.RequireConfirmedAccount  = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── Cookie settings ──────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath         = "/Account/Login";
    options.LogoutPath        = "/Account/Logout";
    options.AccessDeniedPath  = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
});

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IMaterialRequestService, MaterialRequestService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IConsumptionService, ConsumptionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INumberGeneratorService, NumberGeneratorService>();
builder.Services.AddScoped<PurchaseOrderPdfService>();

var app = builder.Build();

// ── Error handling ───────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Railway terminates TLS at load balancer — disable HTTPS redirect to avoid redirect loops
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ───────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// ── Auto-migrate & seed on startup ───────────────────────────────────────────
try
{
    await SeedData.SeedAsync(app.Services, app.Configuration);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error during database migration or seeding");
}

app.Run();

// Expose for integration tests
public partial class Program { }
