using Microsoft.AspNetCore.Identity;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace PtixiakiReservations.Seeders;

public class DataSeeder
{
    private class VenueSeedItem
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("address")] public string Address { get; set; } = string.Empty;
        [JsonPropertyName("city")] public string City { get; set; } = string.Empty;
        [JsonPropertyName("postal_code")] public string PostalCode { get; set; } = string.Empty;
    }

    public static async Task SeedTestDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IServiceProvider serviceProvider)
    {
        // Call basic data seeding first
        BasicDataSeed(context, userManager, roleManager);
    
        // Then call the test data seeder
        await TestDataSeeder.SeedTestDataAsync(serviceProvider);
    }
    
    public static void BasicDataSeed(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            SeedUsers(userManager, roleManager).Wait();
        }

        if (!context.City.Any()){
            SeedCities(context);
            context.SaveChanges();
        }

        if (!context.EventType.Any())
        {
            SeedEventTypes(context);
            context.SaveChanges();
        }

        if (!context.Venue.Any()){
            SeedVenues(context);
            context.SaveChanges();  
        }

        if (!context.VenueCategory.Any())
        {
            SeedVenueCategories(context);
            context.SaveChanges();
        }

        if (!context.SubArea.Any()){
            SeedSubAreas(context);
            context.SaveChanges();
        }

        if (!context.Event.Any()){
            SeedEvents(context);
            context.SaveChanges();
        }

        if (!context.Seat.Any()){
            SeedSeats(context);
            context.SaveChanges();  
        }
    }

    private static async Task SeedUsers(UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        string[] roleNames = { "Admin", "User", "Venue" };
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
                UserName = "admin@admin", Email = "admin@admin", FirstName = "Admin", LastName = "User"
            },
            new ApplicationUser
            {
                UserName = "manager@example.com", Email = "manager@example.com", FirstName = "Manager",
                LastName = "User"
            },
            new ApplicationUser
            {
                UserName = "user@example.com", Email = "user@example.com", FirstName = "Normal", LastName = "User"
            },
        };

        foreach (var user in users)
        {
            var existingUser = await userManager.FindByEmailAsync(user.Email);
            if (existingUser == null)
            {
                // Use different password for admin@admin
                var password = user.Email == "admin@admin" ? "admin123" : "Pass123";
                await userManager.CreateAsync(user, password);

                // Assign roles as an example
                if (user.Email.StartsWith("admin"))
                    await userManager.AddToRoleAsync(user, "Admin");
                else if (user.Email.StartsWith("manager"))
                    await userManager.AddToRoleAsync(user, "Venue");
                else
                    await userManager.AddToRoleAsync(user, "User");
            }
        }
    }

    private static void SeedCities(ApplicationDbContext context)
    {
        var cityNames = new[]
        {
            "Athens",
            "Thessaloniki",
            "Patras",
            "Heraklion",
            "Larissa",
            "Volos",
            "Ioannina",
            "Chania",
            "Rhodes",
            "Kalamata",
            "Corfu"
        };

        foreach (var cityName in cityNames)
        {
            if (!context.City.Any(c => c.Name == cityName))
            {
                context.City.Add(new City { Name = cityName });
            }
        }
    }

    private static void SeedEventTypes(ApplicationDbContext context)
    {
        context.EventType.AddRange(new List<EventType>
        {
            new EventType { Name = "Concert" },
            new EventType { Name = "Theater" },
            new EventType { Name = "Exhibition" },
            new EventType { Name = "Sports" },
            new EventType { Name = "Conference" }
        });
    }

    private static void SeedVenues(ApplicationDbContext context)
    {
        var venuesFilePath = Path.Combine(AppContext.BaseDirectory, "SeedData", "venues.json");

        if (!File.Exists(venuesFilePath))
            return;

        var venueSeeds = JsonSerializer.Deserialize<List<VenueSeedItem>>(
            File.ReadAllText(venuesFilePath)
        );

        if (venueSeeds == null || !venueSeeds.Any())
            return;

        var random = new Random(42);
        var cities = context.City.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        var existingVenueNames = context.Venue
            .Select(v => v.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var venueOwner = context.Users.FirstOrDefault(u => u.Email == "manager@example.com") ?? context.Users.First();

        foreach (var venueSeed in venueSeeds)
        {
            if (string.IsNullOrWhiteSpace(venueSeed.Name))
                continue;

            if (!existingVenueNames.Add(venueSeed.Name.Trim()))
                continue;

            var cityName = ResolveVenueCityName(venueSeed.City);

            if (!cities.TryGetValue(cityName, out var city))
            {
                city = new City { Name = cityName };
                context.City.Add(city);
                cities[cityName] = city;
            }

            context.Venue.Add(new Venue
            {
                Name = venueSeed.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(venueSeed.Address) ? "Unknown address" : venueSeed.Address.Trim(),
                City = city,
                PostalCode = string.IsNullOrWhiteSpace(venueSeed.PostalCode) ? "00000" : venueSeed.PostalCode.Trim(),
                Phone = CreateRandomPhone(random),
                VenueUrl = "www.example.com",
                SocialMediaUrl = "www.example.com",
                UserId = venueOwner.Id,
                imgUrl = $"/images/venues/venue{random.Next(1, 6)}.jpg"
            });
        }
    }

    private static string ResolveVenueCityName(string cityName)
    {
        var cityKey = NormalizeCityKey(cityName);

        var cityAliases = new Dictionary<string, string>
        {
            { "ATHENS", "Athens" },
            { "THESSALONIKI", "Thessaloniki" },
            { "PATRAS", "Patras" },
            { "HERAKLION", "Heraklion" },
            { "LARISSA", "Larissa" },
            { "VOLOS", "Volos" },
            { "IOANNINA", "Ioannina" },
            { "CHANIA", "Chania" },
            { "RHODES", "Rhodes" },
            { "KALAMATA", "Kalamata" },
            { "CORFU", "Corfu" },
            { "\u0391\u0398\u0397\u039D\u0391", "Athens" },
            { "\u03A0\u0395\u0399\u03A1\u0391\u0399\u0391\u03A3", "Athens" },
            { "\u0393\u0391\u039B\u0391\u03A4\u03A3\u0399", "Athens" },
            { "\u0398\u0395\u03A3\u03A3\u0391\u039B\u039F\u039D\u0399\u039A\u0397", "Thessaloniki" },
            { "\u03A0\u0391\u03A4\u03A1\u0391", "Patras" },
            { "\u0397\u03A1\u0391\u039A\u039B\u0395\u0399\u039F", "Heraklion" },
            { "\u039B\u0391\u03A1\u0399\u03A3\u0391", "Larissa" },
            { "\u0399\u03A9\u0391\u039D\u039D\u0399\u039D\u0391", "Ioannina" },
            { "\u03A7\u0391\u039D\u0399\u0391", "Chania" },
            { "\u03A1\u039F\u0394\u039F\u03A3", "Rhodes" },
            { "\u039A\u0391\u039B\u0391\u039C\u0391\u03A4\u0391", "Kalamata" },
            { "\u039A\u0395\u03A1\u039A\u03A5\u03A1\u0391", "Corfu" }
        };

        return cityAliases.TryGetValue(cityKey, out var resolvedCityName)
            ? resolvedCityName
            : string.IsNullOrWhiteSpace(cityName) ? "Athens" : cityName.Trim();
    }

    private static string NormalizeCityKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static string CreateRandomPhone(Random random)
    {
        return $"2{random.Next(100000000, 999999999)}";
    }


    private static void SeedSubAreas(ApplicationDbContext context)
    {
        var venues = context.Venue.ToList();
        var existingSubAreas = context.SubArea.ToList();
        var subAreas = new List<SubArea>();

        foreach (var venue in venues)
        {
            void AddSubAreaIfMissing(string areaName, decimal width, decimal height, decimal top, decimal left, string desc)
            {
                if (existingSubAreas.Any(sa => sa.VenueId == venue.Id && sa.AreaName == areaName))
                    return;

                var subArea = new SubArea
                {
                    AreaName = areaName,
                    Width = width,
                    Height = height,
                    Top = top,
                    Left = left,
                    Rotate = 0,
                    Desc = desc,
                    VenueId = venue.Id
                };

                subAreas.Add(subArea);
                existingSubAreas.Add(subArea);
            }

            AddSubAreaIfMissing("Main Hall", 500, 300, 0, 0, "Primary area in venue");
            AddSubAreaIfMissing("Balcony", 400, 150, 310, 0, "Balcony area in venue");
        }

        context.SubArea.AddRange(subAreas);
    }

    private static void SeedEvents(ApplicationDbContext context)
    {
        const int targetParentEventCount = 1000;

        var eventsFilePath = Path.Combine(AppContext.BaseDirectory, "SeedData", "events.json");

        if (!File.Exists(eventsFilePath))
            return;

        var eventNamesByType = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
            File.ReadAllText(eventsFilePath)
        );

        if (eventNamesByType == null || !eventNamesByType.Any())
            return;

        var random = new Random();

        var venues = context.Venue.ToList();
        var types = context.EventType.ToList();
        var subAreas = context.SubArea.ToList();

        if (!venues.Any() || !types.Any())
            return;

        var parentEvents = context.Event
            .Where(e => e.ParentEventId == null)
            .ToList();

        var parentEventsToCreate = targetParentEventCount - parentEvents.Count;
        var newParentEvents = new List<Event>();

        for (int i = 1; i <= parentEventsToCreate; i++)
        {
            var type = types[random.Next(types.Count)];
            var typeKey = ResolveEventTypeKey(type);

            if (!eventNamesByType.ContainsKey(typeKey) || !eventNamesByType[typeKey].Any())
                continue;

            var venue = venues[random.Next(venues.Count)];

            var start = DateTime.Now
                .AddDays(random.Next(7, 365))
                .Date
                .AddHours(9);

            var durationDays = random.Next(3, 11);
            var end = start.Date
                .AddDays(durationDays - 1)
                .AddHours(23);

            var baseEventName = eventNamesByType[typeKey][random.Next(eventNamesByType[typeKey].Count)];
            var eventName = $"{baseEventName}";

            newParentEvents.Add(new Event
            {
                Name = eventName,
                StartDateTime = start,
                EndTime = end,
                EventTypeId = type.Id,
                VenueId = venue.Id,
                SubAreaId = null,
                ImagePath = $"/images/events/event{random.Next(1, 5)}.jpg"
            });
        }

        if (newParentEvents.Any())
        {
            context.Event.AddRange(newParentEvents);
            context.SaveChanges();
            parentEvents.AddRange(newParentEvents);
        }

        var childEvents = context.Event
            .Where(e => e.ParentEventId.HasValue)
            .ToList();

        var childDaysByParentId = childEvents
            .GroupBy(e => e.ParentEventId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.StartDateTime.Date).ToHashSet()
            );

        var childEventsToCreate = new List<Event>();

        foreach (var parentEvent in parentEvents)
        {
            var type = types.FirstOrDefault(t => t.Id == parentEvent.EventTypeId);

            if (type == null)
                continue;

            var typeKey = ResolveEventTypeKey(type);

            if (!eventNamesByType.ContainsKey(typeKey) || !eventNamesByType[typeKey].Any())
                continue;

            if (parentEvent.EndTime.Date <= parentEvent.StartDateTime.Date)
            {
                parentEvent.StartDateTime = parentEvent.StartDateTime.Date.AddHours(9);
                parentEvent.EndTime = parentEvent.StartDateTime.Date
                    .AddDays(random.Next(3, 11) - 1)
                    .AddHours(23);
            }

            if (!childDaysByParentId.TryGetValue(parentEvent.Id, out var existingChildDays))
            {
                existingChildDays = new HashSet<DateTime>();
                childDaysByParentId[parentEvent.Id] = existingChildDays;
            }

            var venueSubAreas = subAreas
                .Where(s => s.VenueId == parentEvent.VenueId)
                .ToList();

            for (var day = parentEvent.StartDateTime.Date; day <= parentEvent.EndTime.Date; day = day.AddDays(1))
            {
                if (!existingChildDays.Add(day))
                    continue;

                var childStart = day
                    .AddHours(random.Next(10, 21))
                    .AddMinutes(random.Next(0, 4) * 15);

                var childDurationHours = random.Next(2, 6);
                var childEnd = childStart.AddHours(childDurationHours);

                if (childEnd.Date > day)
                    childEnd = day.AddHours(23);

                var childEventName = eventNamesByType[typeKey][random.Next(eventNamesByType[typeKey].Count)];
                var subArea = venueSubAreas.Any()
                    ? venueSubAreas[random.Next(venueSubAreas.Count)]
                    : null;

                childEventsToCreate.Add(new Event
                {
                    Name = $"{childEventName} - Day {(day - parentEvent.StartDateTime.Date).Days + 1}",
                    StartDateTime = childStart,
                    EndTime = childEnd,
                    EventTypeId = parentEvent.EventTypeId,
                    VenueId = parentEvent.VenueId,
                    SubAreaId = subArea?.Id,
                    ParentEventId = parentEvent.Id,
                    ImagePath = null
                });
            }
        }

        context.Event.AddRange(childEventsToCreate);
    }

    private static string ResolveEventTypeKey(EventType eventType)
    {
        var eventTypeKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Concert", "1" },
            { "Theater", "2" },
            { "Exhibition", "3" },
            { "Sports", "4" },
            { "Sport", "4" },
            { "Conference", "5" }
        };

        return eventTypeKeys.TryGetValue(eventType.Name, out var typeKey)
            ? typeKey
            : eventType.Id.ToString();
    }

    private static void SeedVenueCategories(ApplicationDbContext context)
    {
        var etypes = context.EventType.ToList();
        var venues = context.Venue.ToList();
        var categories = new List<VenueCategory>();

        if (!etypes.Any() || !venues.Any())
            return;

        var random = new Random();
        foreach(var venue in venues)
        {
            int randomIndex = random.Next(0, etypes.Count);
            if (randomIndex == -1)
            {
                categories.Add(new VenueCategory
                {
                    VenueId = venue.Id,
                    CategoryId = null
                });
            }
            else
            {
                var randomET = etypes
                .OrderBy(e => random.Next())
                .Take(randomIndex)
                .ToList();
                foreach(var type in randomET)
                {
                    
                    categories.Add(new VenueCategory
                    {
                        VenueId = venue.Id,
                        CategoryId = type.Id
                    });
                }
            }

        }
        context.VenueCategory.AddRange(categories);
    }

    private static void SeedSeats(ApplicationDbContext context)
    {
        var subAreas = context.SubArea.ToList();
        var subAreaIdsWithSeats = context.Seat
            .Select(s => s.SubAreaId)
            .Distinct()
            .ToHashSet();
        var seats = new List<Seat>();

        foreach (var area in subAreas)
        {
            if (subAreaIdsWithSeats.Contains(area.Id))
                continue;

            // Define proper spacing between seats
            var seatSpacingX = 70m; // Horizontal spacing between seats
            var seatSpacingY = 75m; // Vertical spacing between rows
            var startX = 30m; // Starting X position
            var startY = 30m; // Starting Y position

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    seats.Add(new Seat
                    {
                        Name = $"{(char)('A' + row)}{col + 1:D2}",
                        X = startX + (col * seatSpacingX),
                        Y = startY + (row * seatSpacingY),
                        SubAreaId = area.Id,
                        Available = true,
                    });
                }
            }
        }

        context.Seat.AddRange(seats);
    }
}
