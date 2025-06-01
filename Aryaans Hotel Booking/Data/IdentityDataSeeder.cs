using Aryaans_Hotel_Booking.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aryaans_Hotel_Booking.Data
{
    public static class IdentityDataSeeder
    {
        public static async Task SeedRolesAndAdminUserAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("IdentityDataSeeder");

            string adminRoleName = "Admin";
            string adminEmail = "admin@gmail.com"; 
            string adminUsername = "admin";          
            string adminPassword = "AdminPassword123";

            logger.LogInformation("Attempting to seed roles and admin user.");

            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                logger.LogInformation($"Creating role: {adminRoleName}");
                await roleManager.CreateAsync(new IdentityRole(adminRoleName));
            }
            else
            {
                logger.LogInformation($"Role '{adminRoleName}' already exists.");
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation($"Creating admin user: {adminEmail}");
                adminUser = new ApplicationUser
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var createUserResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createUserResult.Succeeded)
                {
                    logger.LogInformation($"Admin user {adminEmail} created successfully. Assigning to '{adminRoleName}' role.");
                    await userManager.AddToRoleAsync(adminUser, adminRoleName);
                }
                else
                {
                    foreach (var error in createUserResult.Errors)
                    {
                        logger.LogError($"Error creating admin user {adminEmail}: {error.Description}");
                    }
                }
            }
            else
            {
                logger.LogInformation($"Admin user {adminEmail} already exists.");
                if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
                {
                    logger.LogInformation($"Assigning existing admin user {adminEmail} to '{adminRoleName}' role.");
                    await userManager.AddToRoleAsync(adminUser, adminRoleName);
                }
                else
                {
                    logger.LogInformation($"Admin user {adminEmail} is already in '{adminRoleName}' role.");
                }
            }
            logger.LogInformation("Role and admin user seeding process completed.");
        }
    }
}