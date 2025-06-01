using Microsoft.EntityFrameworkCore;
using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aryaans_Hotel_Booking.Data.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddResponseCaching();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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

        //logger.LogInformation("Attempting to seed hotel data via HotelDataSeeder...");
        //await HotelDataSeeder.SeedHotelsAsync(context, webHostEnvironment);
        //logger.LogInformation("Hotel data seeding process via HotelDataSeeder completed.");

        logger.LogInformation("Attempting to seed Identity roles and admin user...");
        await IdentityDataSeeder.SeedRolesAndAdminUserAsync(services);
        logger.LogInformation("Identity roles and admin user seeding process completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or data seeding (including Identity).");
    }
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();