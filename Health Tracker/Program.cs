using HealthTracker.Data;
using HealthTracker.Models;
using HealthTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
Console.WriteLine("Images folder exists: " + Directory.Exists(imagePath));
if (Directory.Exists(imagePath))
{
    foreach (var file in Directory.GetFiles(imagePath))
        Console.WriteLine("Found image: " + file);
}

Console.WriteLine("DB: " + builder.Configuration.GetConnectionString("DefaultConnection"));
Console.WriteLine("DB2: " + builder.Configuration["DATABASE_URL"]);

// Add services
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options=>
{ options.SignIn.RequireConfirmedEmail = false; })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddRazorPages();
builder.Services.AddScoped<EmailService>();

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        if (db.Database.GetPendingMigrations().Any())
        {
            db.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration error: " + ex.Message);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();