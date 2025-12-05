using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// MVC - require authorization for controllers by default
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});
// Razor Pages
builder.Services.AddRazorPages(options =>
{
    // Require authorization for Razor Pages in the root folder
    options.Conventions.AuthorizeFolder("/");

    // Allow anonymous for Identity UI area pages (login/register/forgot password/etc.)
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Logout");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/AccessDenied");
});

// Database
// Use SQL Server (LocalDB) by default since project references SQL Server provider and DefaultConnection is configured.
builder.Services.AddDbContext<SporSalonuDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (المهم جداً لإكمال Add Identity)
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<SporSalonuDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Cookie settings - use Identity UI routes
builder.Services.ConfigureApplicationCookie(options =>
{
    // Use the Identity UI endpoints so built-in pages work consistently
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Authorization Policies (Optional)
builder.Services.AddAuthorization(options =>
{
    // Remove global FallbackPolicy that caused redirect loops for Identity pages
    // Keep role-based policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("customer"));
});

var app = builder.Build();

// Auto apply migration (keep for development; consider removing or gating in production)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SporSalonuDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Error Pages
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages (required for Identity UI and Pages)
app.MapRazorPages();

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Area Route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.Run();



/*
 username : b211210569@ogr.sakarya.edu.tr
 PASSWORD : Muhammed.963
 
 */