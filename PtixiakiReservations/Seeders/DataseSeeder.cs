using Microsoft.AspNetCore.Identity;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Seeders;

public class DatabaseSeeder
{
    public static void SeedData(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            SeedUsers(userManager, roleManager).Wait();
        }

        if (!context.Venue.Any())
        {
            SeedVenues(context);
        }

        if (!context.SubArea.Any())
        {
            SeedSubAreas(context);
        }

        if (!context.Event.Any())
        {
            SeedEvents(context);
        }

        if (!context.Seat.Any())
        {
            SeedSeats(context);
        }

        context.SaveChanges();
    }

    private static async Task SeedUsers(UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        string[] roleNames = { "Admin", "User", "Manager" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName, $"{roleName} role", DateTime.UtcNow));
            }
        }

        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                UserName = "admin@example.com", Email = "admin@example.com", FirstName = "Admin", LastName = "User"
            },
            new ApplicationUser
            {
                UserName = "user1@example.com", Email = "user1@example.com", FirstName = "First", LastName = "User"
            },
            new ApplicationUser
            {
                UserName = "manager@example.com", Email = "manager@example.com", FirstName = "Manager",
                LastName = "User"
            },
        };

        foreach (var user in users)
        {
            var existingUser = await userManager.FindByEmailAsync(user.Email);
            if (existingUser == null)
            {
                await userManager.CreateAsync(user, "Pass123!"); // Change password here as required

                // Assign roles as an example
                if (user.Email.StartsWith("admin"))
                    await userManager.AddToRoleAsync(user, "Admin");
                else if (user.Email.StartsWith("manager"))
                    await userManager.AddToRoleAsync(user, "Manager");
                else
                    await userManager.AddToRoleAsync(user, "User");
            }
        }
    }

    private static void SeedVenues(ApplicationDbContext context)
    {
        context.Venue.AddRange(new List<Venue>
        {
            new Venue
            {
                Name = "Concert Hall", Address = "123 Main St", CityId = 1, PostalCode = "12345",
                Phone = "1234567890", UserId = context.Users.First().Id, imgUrl = "/images/venue1.jpg"
            },
            new Venue
            {
                Name = "Exhibition Center", Address = "456 Market St", CityId = 1, PostalCode = "67890",
                Phone = "0987654321", UserId = context.Users.First().Id, imgUrl = "/images/venue2.jpg"
            },
        });
    }

    private static void SeedSubAreas(ApplicationDbContext context)
    {
        var venues = context.Venue.ToList();
        var subAreas = new List<SubArea>();

        foreach (var venue in venues)
        {
            subAreas.Add(new SubArea
            {
                AreaName = "Main Hall",
                Width = 500,
                Height = 300,
                Top = 0,
                Left = 0,
                Rotate = 0,
                Desc = "Primary area in venue",
                VenueId = venue.Id
            });

            subAreas.Add(new SubArea
            {
                AreaName = "Balcony",
                Width = 400,
                Height = 150,
                Top = 310,
                Left = 0,
                Rotate = 0,
                Desc = "Balcony area in venue",
                VenueId = venue.Id
            });
        }

        context.SubArea.AddRange(subAreas);
    }

    private static void SeedEvents(ApplicationDbContext context)
    {
        var venues = context.Venue.ToList();
        var types = context.EventType.ToList();

        context.Event.AddRange(new List<Event>
        {
            new Event
            {
                Name = "Rock concert 2024",
                StartDateTime = DateTime.Now.AddMonths(1),
                EndTime = DateTime.Now.AddMonths(1).AddHours(4),
                EventTypeId = types.First().Id,
                VenueId = venues.First().Id
            },
            new Event
            {
                Name = "Art Exhibition",
                StartDateTime = DateTime.Now.AddMonths(2),
                EndTime = DateTime.Now.AddMonths(2).AddHours(5),
                EventTypeId = types.Last().Id,
                VenueId = venues.Last().Id
            }
        });
    }

    private static void SeedSeats(ApplicationDbContext context)
    {
        var subAreas = context.SubArea.ToList();
        var seats = new List<Seat>();

        foreach (var area in subAreas)
        {
            for (int row = 1; row <= 5; row++)
            {
                for (int col = 1; col <= 10; col++)
                {
                    seats.Add(new Seat
                    {
                        Name = "Seeded seat",
                        Y = col,
                        X = row,
                        SubAreaId = area.Id,
                        Available = true,
                    });
                }
            }
        }

        context.Seat.AddRange(seats);
    }
}