using Microsoft.EntityFrameworkCore;
using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Services; // Assuming this is used, otherwise can be removed
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddResponseCaching();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Seed database
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

        logger.LogInformation("Attempting to seed hotel data via HotelDataSeeder...");
        await HotelDataSeeder.SeedHotelsAsync(context, webHostEnvironment); // Make sure HotelDataSeeder is correctly defined
        logger.LogInformation("Hotel data seeding process via HotelDataSeeder completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // For unhandled exceptions (typically 500 series)
    app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}"); // << ADDED for other HTTP error codes
    app.UseHsts();
}
else
{
    // In development, you might want to see developer exception pages for unhandled exceptions
    app.UseDeveloperExceptionPage();
    // But still use custom status code pages for testing those specific error views
    app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}"); // << ALSO ADDED for development testing
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseResponseCaching(); // Already added for Step 2

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();