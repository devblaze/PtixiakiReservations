using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Seeders;

public static class ApplicationDbSeed
{
    private static readonly string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var elasticSearchService = serviceProvider.GetRequiredService<IElasticSearch>();

        // Ensure Roles and Admin User
        await SeedAsync(userManager, roleManager);

        // Seed Venue and Event Data
        // await SeedVenuesAndEventsAsync(userManager, dbContext, elasticSearchService);
    }

    public static async Task SeedAsync(UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Ensure Roles
        var roles = ExtractRolesFromControllers();

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var newRole = new ApplicationRole
                {
                    Name = role,
                    description = $"Auto-generated role for {role}",
                    creationDate = DateTime.UtcNow
                };

                await roleManager.CreateAsync(newRole);
            }
        }

        // Ensure Admin User Exists
        await EnsureAdminUserExists(userManager, roleManager);
    }

    private static async Task EnsureAdminUserExists(UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        const string adminEmail = "admin@admin";
        const string adminPassword = "admin123";

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
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }
            else
            {
                throw new Exception("Failed to create admin user: " +
                                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
            {
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }
        }
        Console.WriteLine("Admin user ensured.");
    }

    private static List<string> ExtractRolesFromControllers()
    {
        return new List<string>
        {
            AdminRole,
            "Venue",
        };
    }

    private static async Task SeedVenuesAndEventsAsync(UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext, IElasticSearch elasticSearchService)
    {
        const string adminEmail = "admin@admin";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            throw new Exception("Admin user not found. Run the role and user seeder first.");
        }

        // Seed cities
        if (!dbContext.City.Any())
        {
            dbContext.City.AddRange(new List<City>
            {
                new City { Name = "New York" },
                new City { Name = "Los Angeles" },
                new City { Name = "San Francisco" }
            });

            await dbContext.SaveChangesAsync();
        }

        var cities = dbContext.City.ToList();

        // Seed FamilyEvents
        if (!dbContext.FamilyEvent.Any())
        {
            var familyEvents = new List<FamilyEvent>
            {
                new FamilyEvent { Name = "Annual Music Festival" },
                new FamilyEvent { Name = "Tech Meetup" },
                new FamilyEvent { Name = "Science Fair" }
            };

            dbContext.FamilyEvent.AddRange(familyEvents);
            await dbContext.SaveChangesAsync();
        }

        var familyEventsFromDb = dbContext.FamilyEvent.ToList();

        // Seed venues
        if (!dbContext.Venue.Any())
        {
            dbContext.Venue.AddRange(new List<Venue>
            {
                new Venue
                {
                    Name = "Grand Concert Hall",
                    Address = "123 Grand St",
                    City = cities.FirstOrDefault(c => c.Name == "New York"),
                    PostalCode = "10001",
                    Phone = "123-456-7890",
                    UserId = adminUser.Id,
                    imgUrl = "concert.jpg"
                },
                new Venue
                {
                    Name = "Downtown Theater",
                    Address = "456 Elm St",
                    City = cities.FirstOrDefault(c => c.Name == "Los Angeles"),
                    PostalCode = "90001",
                    Phone = "555-555-1234",
                    UserId = adminUser.Id,
                    imgUrl = "theater.jpg"
                }
            });

            await dbContext.SaveChangesAsync();
        }

        var venuesFromDb = dbContext.Venue.ToList();

        // Seed events
        if (!dbContext.Event.Any())
        {
            dbContext.Event.AddRange(new List<Event>
            {
                new Event
                {
                    Name = "Rock Concert",
                    StartDateTime = DateTime.Now.AddDays(1),
                    EndTime = DateTime.Now.AddDays(1).AddHours(3),
                    Venue = venuesFromDb.FirstOrDefault(v => v.Name == "Grand Concert Hall"),
                    FamilyEvent = familyEventsFromDb.FirstOrDefault(fe => fe.Name == "Annual Music Festival")
                },
                new Event
                {
                    Name = "Broadway Show",
                    StartDateTime = DateTime.Now.AddDays(2),
                    EndTime = DateTime.Now.AddDays(2).AddHours(2),
                    Venue = venuesFromDb.FirstOrDefault(v => v.Name == "Grand Concert Hall"),
                    FamilyEvent = familyEventsFromDb.FirstOrDefault(fe => fe.Name == "Tech Meetup")
                },
                new Event
                {
                    Name = "Tech Conference",
                    StartDateTime = DateTime.Now.AddDays(3),
                    EndTime = DateTime.Now.AddDays(3).AddHours(4),
                    Venue = venuesFromDb.FirstOrDefault(v => v.Name == "Downtown Theater"),
                    FamilyEvent = familyEventsFromDb.FirstOrDefault(fe => fe.Name == "Science Fair")
                }
            });

            await dbContext.SaveChangesAsync();
        }
    }
}