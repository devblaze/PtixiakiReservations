using Microsoft.AspNetCore.Identity;
using PtixiakiReservations.Models;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.Seeders
{
    public static class TestUserSeeder
    {
        public static async Task SeedTestUsersAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // Make sure the "Venue" role exists
            if (!await roleManager.RoleExistsAsync("Venue"))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = "Venue",
                    description = "Venue manager role",
                    creationDate = DateTime.UtcNow
                });
            }

            // Create a venue manager
            var venueManagerUser = new ApplicationUser
            {
                UserName = "venue@test.com",
                Email = "venue@test.com",
                FirstName = "Venue",
                LastName = "Manager",
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(venueManagerUser.Email) == null)
            {
                var result = await userManager.CreateAsync(venueManagerUser, "Test123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(venueManagerUser, "Venue");
                    Console.WriteLine("Venue manager user created successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to create venue manager: {string.Join(", ", result.Errors)}");
                }
            }
            else
            {
                Console.WriteLine("Venue manager already exists.");
            }

            // Create a normal user
            var normalUser = new ApplicationUser
            {
                UserName = "user@test.com",
                Email = "user@test.com",
                FirstName = "Normal",
                LastName = "User",
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(normalUser.Email) == null)
            {
                var result = await userManager.CreateAsync(normalUser, "Test123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(normalUser, "User");
                    Console.WriteLine("Normal user created successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to create normal user: {string.Join(", ", result.Errors)}");
                }
            }
            else
            {
                Console.WriteLine("Normal user already exists.");
            }
        }
    }
}