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

public static class TestDataSeeder
{
    private static readonly Random _random = new Random();

    public static async Task SeedTestDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var elasticSearchService = services.GetRequiredService<IElasticSearch>();

        // Get admin user for assigning ownership
        var adminUser = await userManager.FindByEmailAsync("admin@admin");
        if (adminUser == null)
        {
            Console.WriteLine("Admin user not found. Please run the regular seeder first.");
            return;
        }

        // Seed Event Types if needed
        await SeedEventTypesAsync(context);

        // Seed Cities
        var cities = await SeedCitiesAsync(context);

        // Seed Family Events
        var familyEvents = await SeedFamilyEventsAsync(context);

        // Seed Venues (15-20 venues)
        var venues = await SeedVenuesAsync(context, adminUser.Id, cities);

        // Seed SubAreas for each venue (2-5 per venue)
        var subAreas = await SeedSubAreasAsync(context, venues);

        // Seed Seats for each SubArea
        await SeedSeatsAsync(context, subAreas);

        // Seed Events (100-200 events across all venues)
        var events = await SeedEventsAsync(context, venues, familyEvents);

        // Index events in Elasticsearch
        await IndexEventsInElasticsearchAsync(elasticSearchService, events);

        Console.WriteLine("Test data seeding completed successfully!");
    }

    private static async Task<List<EventType>> SeedEventTypesAsync(ApplicationDbContext context)
    {
        if (await context.EventType.AnyAsync())
            return await context.EventType.ToListAsync();

        var eventTypes = new List<EventType>
        {
            new EventType { Name = "Concert" },
            new EventType { Name = "Theater" },
            new EventType { Name = "Conference" },
            new EventType { Name = "Exhibition" },
            new EventType { Name = "Sport" },
            new EventType { Name = "Workshop" },
            new EventType { Name = "Party" },
            new EventType { Name = "Networking" }
        };

        await context.EventType.AddRangeAsync(eventTypes);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {eventTypes.Count} event types.");
        return eventTypes;
    }

    private static async Task<List<City>> SeedCitiesAsync(ApplicationDbContext context)
    {
        if (await context.City.AnyAsync())
            return await context.City.ToListAsync();

        var cities = new List<City>
        {
            new City { Name = "New York" },
            new City { Name = "Los Angeles" },
            new City { Name = "Chicago" },
            new City { Name = "Houston" },
            new City { Name = "Phoenix" },
            new City { Name = "Philadelphia" },
            new City { Name = "San Antonio" },
            new City { Name = "San Diego" },
            new City { Name = "Dallas" },
            new City { Name = "San Jose" },
            new City { Name = "Austin" },
            new City { Name = "Boston" },
            new City { Name = "Las Vegas" },
            new City { Name = "Seattle" },
            new City { Name = "Denver" }
        };

        await context.City.AddRangeAsync(cities);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {cities.Count} cities.");
        return cities;
    }

    private static async Task<List<FamilyEvent>> SeedFamilyEventsAsync(ApplicationDbContext context)
    {
        if (await context.FamilyEvent.AnyAsync())
            return await context.FamilyEvent.ToListAsync();

        var familyEvents = new List<FamilyEvent>
        {
            new FamilyEvent { Name = "Summer Music Festival" },
            new FamilyEvent { Name = "Tech Conference Series" },
            new FamilyEvent { Name = "Holiday Theater Series" },
            new FamilyEvent { Name = "Sports League Season" },
            new FamilyEvent { Name = "Art Exhibition Tour" },
            new FamilyEvent { Name = "Comedy Tour" },
            new FamilyEvent { Name = "Book Reading Series" },
            new FamilyEvent { Name = "Film Festival" },
            new FamilyEvent { Name = "Dance Performance Series" },
            new FamilyEvent { Name = "Food and Wine Festival" }
        };

        await context.FamilyEvent.AddRangeAsync(familyEvents);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {familyEvents.Count} family events.");
        return familyEvents;
    }

    private static async Task<List<Venue>> SeedVenuesAsync(ApplicationDbContext context, string userId,
        List<City> cities)
    {
        if (await context.Venue.CountAsync() > 15)
            return await context.Venue.ToListAsync();

        var venueNames = new List<string>
        {
            "Grand Theater", "Symphony Hall", "The Arena", "Exhibition Center",
            "Conference Center", "The Auditorium", "Concert Hall", "Stadium",
            "Cultural Center", "Music Venue", "The Lounge", "Arts Club",
            "Performance Space", "Event Center", "The Gallery", "Festival Grounds",
            "Comedy Club", "Jazz Club", "The Ballroom", "Opera House"
        };

        var streets = new List<string>
        {
            "Main St", "Broadway", "First Ave", "Park Ave", "Oak St",
            "Maple Dr", "Washington Blvd", "Lincoln Ave", "Market St",
            "Grand Blvd", "Sunset Blvd", "River Road", "Central Ave"
        };

        var venues = new List<Venue>();
        var imageUrls = new List<string>
            { "concert.jpg", "theater.jpg", "conference.jpg", "venue.jpg", "stadium.jpg" };

        // Create 15-20 venues
        var numVenues = _random.Next(15, 21);

        for (int i = 0; i < numVenues; i++)
        {
            var venueName = GetUniqueItem(venueNames, venues.Select(v => v.Name).ToList());
            var city = cities[_random.Next(cities.Count)];

            var venue = new Venue
            {
                Name = venueName,
                Address = $"{_random.Next(100, 10000)} {streets[_random.Next(streets.Count)]}",
                City = city,
                PostalCode = $"{_random.Next(10000, 100000)}",
                Phone = $"{_random.Next(100, 1000)}-{_random.Next(100, 1000)}-{_random.Next(1000, 10000)}",
                UserId = userId,
                imgUrl = imageUrls[_random.Next(imageUrls.Count)]
            };

            venues.Add(venue);
        }

        await context.Venue.AddRangeAsync(venues);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {venues.Count} venues.");
        return venues;
    }

    private static async Task<List<SubArea>> SeedSubAreasAsync(ApplicationDbContext context, List<Venue> venues)
    {
        if (await context.SubArea.CountAsync() > venues.Count * 2)
            return await context.SubArea.ToListAsync();

        var subAreaNames = new List<string>
        {
            "Main Floor", "Balcony", "VIP Section", "Orchestra", "Mezzanine",
            "Terrace", "Box Seats", "Front Section", "Middle Section", "Back Section",
            "Stage Left", "Stage Right", "Upper Level", "Lower Level", "Reserved Section"
        };

        var subAreas = new List<SubArea>();

        foreach (var venue in venues)
        {
            // Each venue gets 2-5 sub-areas
            var numSubAreas = _random.Next(2, 6);

            for (int i = 0; i < numSubAreas; i++)
            {
                var nameIndex = _random.Next(subAreaNames.Count);
                var subAreaName = i == 0
                    ? "Main Floor" // Always have at least one "Main Floor"
                    : subAreaNames[nameIndex];

                var subArea = new SubArea
                {
                    AreaName = $"{subAreaName} {(i > 0 ? i.ToString() : "")}".Trim(),
                    Desc = $"Seating area in {venue.Name}",
                    VenueId = venue.Id,
                    Width = (decimal)_random.Next(300, 601),
                    Height = (decimal)_random.Next(200, 401),
                    Top = (decimal)_random.Next(0, 50),
                    Left = (decimal)_random.Next(0, 50),
                    Rotate = 0
                };

                subAreas.Add(subArea);
            }
        }

        await context.SubArea.AddRangeAsync(subAreas);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {subAreas.Count} sub-areas across {venues.Count} venues.");
        return subAreas;
    }

    private static async Task SeedSeatsAsync(ApplicationDbContext context, List<SubArea> subAreas)
    {
        // Check if we already have a good number of seats
        if (await context.Seat.CountAsync() > subAreas.Count * 20)
            return;

        var seats = new List<Seat>();

        foreach (var subArea in subAreas)
        {
            // Create a grid of seats for each subarea
            // Number of rows and columns based on the subarea size
            var rows = _random.Next(3, 8);
            var columns = _random.Next(5, 11);

            var seatWidth = 50;
            var seatHeight = 50;
            var horizontalGap = 30;
            var verticalGap = 30;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var x = 50 + col * (seatWidth + horizontalGap);
                    var y = 50 + row * (seatHeight + verticalGap);

                    var seat = new Seat
                    {
                        Name = $"Row:{row + 1}-Col:{col + 1}",
                        X = (decimal)x,
                        Y = (decimal)y,
                        Available = true,
                        SubAreaId = subArea.Id
                    };

                    seats.Add(seat);
                }
            }
        }

        await context.Seat.AddRangeAsync(seats);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {seats.Count} seats across {subAreas.Count} sub-areas.");
    }

    private static async Task<List<Event>> SeedEventsAsync(ApplicationDbContext context, List<Venue> venues,
        List<FamilyEvent> familyEvents)
    {
        // If we already have a lot of events, skip
        if (await context.Event.CountAsync() > 100)
            return await context.Event.ToListAsync();

        var eventNames = new List<string>
        {
            "Summer Symphony", "Rock Festival", "Jazz Night", "Classical Concert",
            "Tech Summit", "Developer Conference", "Industry Expo", "Innovation Forum",
            "Broadway Show", "Shakespeare in the Park", "Comedy Night", "Ballet Performance",
            "Art Exhibition", "Photography Showcase", "Sculpture Garden",
            "Sports Championship", "Tournament Finals", "Charity Run", "Fitness Challenge",
            "Food Festival", "Wine Tasting", "Craft Beer Festival", "Cooking Showcase",
            "Business Networking", "Startup Pitch", "Investor Meeting", "Career Fair"
        };

        var events = new List<Event>();
        var eventTypes = await context.EventType.ToListAsync();

        // Generate 100-200 events
        var numEvents = _random.Next(500, 1000);

        // Generate events for the next 6 months
        var today = DateTime.Now;
        var startDate = today.AddDays(-2);
        var endDate = today.AddDays(7);

        for (int i = 0; i < numEvents; i++)
        {
            var eventDate = GetRandomDateBetween(startDate, endDate);
            var eventType = eventTypes[_random.Next(eventTypes.Count)];
            var venue = venues[_random.Next(venues.Count)];
            var familyEvent = _random.Next(4) == 0 // 25% chance to be part of a family event
                ? familyEvents[_random.Next(familyEvents.Count)]
                : null;

            var eventNameBase = eventNames[_random.Next(eventNames.Count)];
            var eventName = $"{eventNameBase} {(i % 10 == 0 ? i / 10 + 1 : "")}".Trim();

            // Event duration between 1-4 hours
            var durationHours = _random.Next(1, 5);

            var newEvent = new Event
            {
                Name = eventName,
                StartDateTime = eventDate,
                EndTime = eventDate.AddHours(durationHours),
                EventTypeId = eventType.Id,
                VenueId = venue.Id,
                FamilyEventId = familyEvent?.Id
            };

            events.Add(newEvent);
        }

        await context.Event.AddRangeAsync(events);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {events.Count} events.");
        return events;
    }

    private static async Task IndexEventsInElasticsearchAsync(IElasticSearch elasticSearchService,
        List<Event> events)
    {
        try
        {
            // First check if Elasticsearch is reachable
            var indexCreated = await elasticSearchService.CreateIndexIfNotExistsAsync("events");
            if (!indexCreated)
            {
                Console.WriteLine("Warning: Could not create or verify Elasticsearch index. Skipping indexing.");
                return; // Skip indexing if we can't create the index
            }

            // Index in batches to avoid overwhelming the service
            const int batchSize = 50;

            for (int i = 0; i < events.Count; i += batchSize)
            {
                try
                {
                    var batch = events.Skip(i).Take(batchSize).ToList();
                    var result = await elasticSearchService.AddOrUpdateBulkAsync(batch, "events");

                    if (result)
                    {
                        Console.WriteLine($"Indexed events {i + 1} to {Math.Min(i + batchSize, events.Count)}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to index events {i + 1} to {Math.Min(i + batchSize, events.Count)}");
                    }
                }
                catch (Exception batchEx)
                {
                    Console.WriteLine(
                        $"Error indexing batch {i + 1} to {Math.Min(i + batchSize, events.Count)}: {batchEx.Message}");
                }
            }

            Console.WriteLine("Completed indexing events in Elasticsearch.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Elasticsearch indexing: {ex.Message}");
            // Continue with seeding even if Elasticsearch fails
        }
    }

    // Helper methods
    private static DateTime GetRandomDateBetween(DateTime startDate, DateTime endDate)
    {
        var timeSpan = endDate - startDate;
        var randomDays = _random.Next((int)timeSpan.TotalDays);

        // Add random time component (0-23 hours, 0-59 minutes)
        return startDate.AddDays(randomDays)
            .AddHours(_random.Next(9, 20)) // Events between 9am and 8pm
            .AddMinutes(_random.Next(0, 12) * 5); // Minutes in 5-minute increments
    }

    private static string GetUniqueItem(List<string> items, List<string> existingItems)
    {
        string item;
        int attempts = 0;

        do
        {
            item = items[_random.Next(items.Count)];
            attempts++;

            // After several attempts, make it unique by adding a number
            if (attempts > 5)
            {
                item = $"{item} {_random.Next(100)}";
            }
        } while (existingItems.Contains(item) && attempts < 10);

        return item;
    }
}