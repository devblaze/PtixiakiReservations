using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PtixiakiReservations.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Seeders;

public static class ApplicationDbSeed
{
    // Existing Seeder with IServiceProvider
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        // Resolve services from the provider
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Call the overload
        await SeedAsync(userManager, roleManager);
    }

    // Overloaded Seeder directly using UserManager and RoleManager
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Admin Role Configuration
        string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = adminRole });
        }

        // Admin User Configuration
        string adminEmail = "admin@admin";
        string adminPassword = "admin123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }
            else
            {
                throw new Exception("Failed to create admin user: " +
                                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        Console.WriteLine("Admin user created.");
    }
}