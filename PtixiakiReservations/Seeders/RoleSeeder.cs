using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PtixiakiReservations.Models; 

namespace PtixiakiReservations.Seeders;

public static class RoleSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roleNames = { "Admin", "User", "SuperOrganizer", "Event", "Venue" };

        Console.WriteLine("--- STARTING ROLE SEEDER ---");

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var newRole = new ApplicationRole
                {
                    Name = roleName,
                    description = $"{roleName} role",
                    creationDate = DateTime.UtcNow
                };

                var result = await roleManager.CreateAsync(newRole);
                if (result.Succeeded) Console.WriteLine($"SUCCESS: Created Role '{roleName}'");
            }
        }

        string adminEmail = "admin@admin.com";
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

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("SUCCESS: Created Admin user.");
            }
        }
        Console.WriteLine("--- ROLE SEEDER FINISHED ---");
    }
}