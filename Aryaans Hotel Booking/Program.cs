using Microsoft.EntityFrameworkCore;
using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Services; // <-- For HotelDataSeeder
using Microsoft.AspNetCore.Hosting;   // <-- For IWebHostEnvironment
using Microsoft.Extensions.DependencyInjection; // <-- For CreateScope
using Microsoft.Extensions.Logging; // <-- For ILogger

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>(); 
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var webHostEnvironment = services.GetRequiredService<IWebHostEnvironment>();

        logger.LogInformation("Applying database migrations if any...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied (or database up-to-date).");

        logger.LogInformation("Attempting to seed hotel data...");
        await HotelDataSeeder.SeedHotelsAsync(context, webHostEnvironment);
        logger.LogInformation("Hotel data seeding process completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
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

app.UseAuthorization(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();