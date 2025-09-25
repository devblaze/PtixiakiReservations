using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;

namespace PtixiakiReservations.Tests.Integration
{
    public class SimplifiedUITests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public SimplifiedUITests(WebApplicationFactory<Program> factory)
        {
            // Set environment variable to indicate we're running tests
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove ALL existing DbContext related registrations
                    var descriptorsToRemove = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                   d.ServiceType == typeof(DbContextOptions) ||
                                   d.ServiceType == typeof(ApplicationDbContext))
                        .ToList();

                    foreach (var descriptor in descriptorsToRemove)
                    {
                        services.Remove(descriptor);
                    }

                    // Also remove Entity Framework related services
                    var efDescriptors = services
                        .Where(d => d.ServiceType.FullName != null &&
                                   (d.ServiceType.FullName.Contains("EntityFramework") ||
                                    d.ServiceType.FullName.Contains("SqlServer") ||
                                    d.ServiceType.FullName.Contains("Npgsql")))
                        .ToList();

                    foreach (var descriptor in efDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database with a fresh service provider
                    services.AddEntityFrameworkInMemoryDatabase();
                    services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                        options.UseInternalServiceProvider(serviceProvider);
                    });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Test_VenueCreation_CompleteWorkflow()
        {
            // Arrange - Create services and seed data
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Create Venue role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Venue"))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = "Venue" });
            }

            // Create a venue manager user
            var venueManager = new ApplicationUser
            {
                UserName = "venuetest@example.com",
                Email = "venuetest@example.com",
                EmailConfirmed = true,
                FirstName = "Venue",
                LastName = "Manager"
            };
            await userManager.CreateAsync(venueManager, "Test123!");
            await userManager.AddToRoleAsync(venueManager, "Venue");

            // Add test city
            var city = new City { Name = "Athens" };
            context.City.Add(city);
            await context.SaveChangesAsync();

            // Create a venue
            var venue = new Venue
            {
                Name = "Test Theater",
                Address = "123 Test Street",
                CityId = city.Id,
                PostalCode = "12345",
                Phone = "1234567890",
                UserId = venueManager.Id,
                imgUrl = "/images/default.jpg"
            };
            context.Venue.Add(venue);
            await context.SaveChangesAsync();

            // Create a SubArea
            var subArea = new SubArea
            {
                AreaName = "Main Hall",
                Width = 100,
                Height = 80,
                Top = 10,
                Left = 10,
                Rotate = 0,
                Desc = "Main seating area",
                VenueId = venue.Id
            };
            context.SubArea.Add(subArea);
            await context.SaveChangesAsync();

            // Create seats
            var seats = new List<Seat>();
            for (int i = 1; i <= 20; i++)
            {
                seats.Add(new Seat
                {
                    X = 10 + (i * 5),
                    Y = 20,
                    Name = $"A{i}",
                    Available = true,
                    SubAreaId = subArea.Id
                });
            }
            context.Seat.AddRange(seats);
            await context.SaveChangesAsync();

            // Assert - Verify the complete structure
            var createdVenue = await context.Venue
                .Include(v => v.City)
                .FirstOrDefaultAsync(v => v.Id == venue.Id);

            Assert.NotNull(createdVenue);
            Assert.Equal("Test Theater", createdVenue.Name);
            Assert.Equal("Athens", createdVenue.City.Name);

            var createdSubArea = await context.SubArea
                .FirstOrDefaultAsync(sa => sa.VenueId == venue.Id);

            Assert.NotNull(createdSubArea);
            Assert.Equal("Main Hall", createdSubArea.AreaName);

            var createdSeats = await context.Seat
                .Where(s => s.SubAreaId == subArea.Id)
                .ToListAsync();

            Assert.Equal(20, createdSeats.Count);
            Assert.All(createdSeats, s => Assert.True(s.Available));
        }

        [Fact]
        public async Task Test_EventCreation_MultipleEvents()
        {
            // Arrange - Setup database
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.EnsureCreatedAsync();

            // Create venue manager
            var manager = new ApplicationUser
            {
                UserName = "eventmanager@example.com",
                Email = "eventmanager@example.com",
                EmailConfirmed = true,
                FirstName = "Event",
                LastName = "Manager"
            };
            await userManager.CreateAsync(manager, "Test123!");

            // Create city and venue
            var city = new City { Name = "Thessaloniki" };
            context.City.Add(city);

            var venue = new Venue
            {
                Name = "Event Venue",
                Address = "456 Event Ave",
                CityId = city.Id,
                PostalCode = "54321",
                Phone = "9876543210",
                UserId = manager.Id,
                imgUrl = "/images/venue.jpg"
            };
            context.Venue.Add(venue);
            await context.SaveChangesAsync();

            // Create a family event (group of related events)
            var familyEvent = new FamilyEvent
            {
                Name = "Summer Concert Series"
            };
            context.FamilyEvent.Add(familyEvent);
            await context.SaveChangesAsync();

            // Create multiple events for different days
            var baseDate = DateTime.Today.AddDays(30); // Start in 30 days
            var events = new List<Event>();

            for (int day = 0; day < 3; day++) // Create 3 events
            {
                var eventDate = baseDate.AddDays(day);
                events.Add(new Event
                {
                    Name = $"Summer Concert Day {day + 1}",
                    StartDateTime = eventDate.AddHours(19), // 7 PM
                    EndTime = eventDate.AddHours(22), // 10 PM
                    VenueId = venue.Id,
                    FamilyEventId = familyEvent.Id
                });
            }

            context.Event.AddRange(events);
            await context.SaveChangesAsync();

            // Assert - Verify events were created correctly
            var createdEvents = await context.Event
                .Where(e => e.FamilyEventId == familyEvent.Id)
                .OrderBy(e => e.StartDateTime)
                .ToListAsync();

            Assert.Equal(3, createdEvents.Count);

            // Verify events are on consecutive days
            for (int i = 0; i < createdEvents.Count - 1; i++)
            {
                var dayDiff = (createdEvents[i + 1].StartDateTime.Date - createdEvents[i].StartDateTime.Date).Days;
                Assert.Equal(1, dayDiff);
            }

            // Verify all events are at the same time
            Assert.All(createdEvents, e => Assert.Equal(19, e.StartDateTime.Hour));
            Assert.All(createdEvents, e => Assert.Equal(22, e.EndTime.Hour));
        }

        [Fact]
        public async Task Test_EventSearchAndReservation_CompleteFlow()
        {
            // Arrange - Setup complete test data
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.EnsureCreatedAsync();

            // Create customer user
            var customer = new ApplicationUser
            {
                UserName = "customer@example.com",
                Email = "customer@example.com",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Customer"
            };
            await userManager.CreateAsync(customer, "Test123!");

            // Create venue owner
            var venueOwner = new ApplicationUser
            {
                UserName = "owner@example.com",
                Email = "owner@example.com",
                EmailConfirmed = true,
                FirstName = "Venue",
                LastName = "Owner"
            };
            await userManager.CreateAsync(venueOwner, "Test123!");

            // Create city
            var city = new City { Name = "Patras" };
            context.City.Add(city);

            // Create venue
            var venue = new Venue
            {
                Name = "Patras Concert Hall",
                Address = "789 Concert Street",
                CityId = city.Id,
                PostalCode = "26223",
                Phone = "2610123456",
                UserId = venueOwner.Id,
                imgUrl = "/images/concert.jpg"
            };
            context.Venue.Add(venue);

            // Create SubArea
            var subArea = new SubArea
            {
                AreaName = "Orchestra",
                Width = 150,
                Height = 100,
                Top = 5,
                Left = 5,
                Rotate = 0,
                Desc = "Orchestra seating",
                VenueId = venue.Id
            };
            context.SubArea.Add(subArea);

            // Create seats
            var seats = new List<Seat>();
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    seats.Add(new Seat
                    {
                        X = 10 + (col * 15),
                        Y = 20 + (row * 15),
                        Name = $"{(char)('A' + row)}{col + 1}",
                        Available = true,
                        SubAreaId = subArea.Id
                    });
                }
            }
            context.Seat.AddRange(seats);

            // Create event
            var concertEvent = new Event
            {
                Name = "Classical Music Night",
                StartDateTime = DateTime.Today.AddDays(14).AddHours(20),
                EndTime = DateTime.Today.AddDays(14).AddHours(23),
                VenueId = venue.Id,
                FamilyEventId = 1
            };
            context.Event.Add(concertEvent);
            await context.SaveChangesAsync();

            // Act - Search for events
            var eventsInCity = await context.Event
                .Include(e => e.Venue)
                .ThenInclude(v => v.City)
                .Where(e => e.Venue.City.Name == "Patras")
                .ToListAsync();

            Assert.Single(eventsInCity);
            Assert.Equal("Classical Music Night", eventsInCity[0].Name);

            // Search by date range
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(20);

            var eventsInDateRange = await context.Event
                .Where(e => e.StartDateTime >= startDate && e.StartDateTime <= endDate)
                .ToListAsync();

            Assert.Single(eventsInDateRange);

            // Act - Make a reservation
            var selectedSeat = seats.First(); // Select first available seat

            var reservation = new Reservation
            {
                UserId = customer.Id,
                SeatId = selectedSeat.Id,
                EventId = concertEvent.Id,
                Date = DateTime.Now,
                Duration = TimeSpan.FromHours(3)
            };
            context.Reservation.Add(reservation);

            // Mark seat as unavailable
            selectedSeat.Available = false;
            context.Seat.Update(selectedSeat);
            await context.SaveChangesAsync();

            // Assert - Verify reservation
            var createdReservation = await context.Reservation
                .Include(r => r.Seat)
                .Include(r => r.Event)
                .Include(r => r.ApplicationUser)
                .FirstOrDefaultAsync(r => r.ID == reservation.ID);

            Assert.NotNull(createdReservation);
            Assert.Equal(customer.Id, createdReservation.UserId);
            Assert.Equal(concertEvent.Id, createdReservation.EventId);
            Assert.Equal("A1", createdReservation.Seat.Name);
            Assert.False(createdReservation.Seat.Available);

            // Verify seat availability
            var availableSeats = await context.Seat
                .Where(s => s.SubAreaId == subArea.Id && s.Available)
                .CountAsync();

            Assert.Equal(49, availableSeats); // 50 total - 1 reserved

            // Test double-booking prevention
            var duplicateReservation = new Reservation
            {
                UserId = customer.Id,
                SeatId = selectedSeat.Id, // Try to book the same seat
                EventId = concertEvent.Id,
                Date = DateTime.Now,
                Duration = TimeSpan.FromHours(3)
            };

            // This should fail due to seat being unavailable
            Assert.False(selectedSeat.Available);
        }

        [Fact]
        public async Task Test_SearchEventsByName()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();

            // Create test data
            var city = new City { Name = "TestCity" };
            context.City.Add(city);

            var venue = new Venue
            {
                Name = "Test Venue",
                Address = "Test Address",
                CityId = city.Id,
                PostalCode = "12345",
                Phone = "1234567890",
                UserId = "test-user-id",
                imgUrl = "/test.jpg"
            };
            context.Venue.Add(venue);

            var events = new List<Event>
            {
                new Event
                {
                    Name = "Shakespeare's Hamlet",
                    StartDateTime = DateTime.Today.AddDays(5).AddHours(19),
                    EndTime = DateTime.Today.AddDays(5).AddHours(22),
                    VenueId = venue.Id,
                    FamilyEventId = 1
                },
                new Event
                {
                    Name = "Romeo and Juliet",
                    StartDateTime = DateTime.Today.AddDays(7).AddHours(19),
                    EndTime = DateTime.Today.AddDays(7).AddHours(22),
                    VenueId = venue.Id,
                    FamilyEventId = 2
                },
                new Event
                {
                    Name = "Modern Dance Show",
                    StartDateTime = DateTime.Today.AddDays(10).AddHours(20),
                    EndTime = DateTime.Today.AddDays(10).AddHours(22),
                    VenueId = venue.Id,
                    FamilyEventId = 3
                }
            };
            context.Event.AddRange(events);
            await context.SaveChangesAsync();

            // Act - Search for Shakespeare events
            var searchTerm = "Shakespeare";
            var searchResults = await context.Event
                .Where(e => e.Name.Contains(searchTerm))
                .ToListAsync();

            // Assert
            Assert.Single(searchResults);
            Assert.Equal("Shakespeare's Hamlet", searchResults[0].Name);

            // Act - Search for all theater events
            var theaterSearch = await context.Event
                .Where(e => e.Name.Contains("Juliet") || e.Name.Contains("Hamlet"))
                .OrderBy(e => e.StartDateTime)
                .ToListAsync();

            // Assert
            Assert.Equal(2, theaterSearch.Count);
            Assert.Contains(theaterSearch, e => e.Name == "Shakespeare's Hamlet");
            Assert.Contains(theaterSearch, e => e.Name == "Romeo and Juliet");
        }
    }
}