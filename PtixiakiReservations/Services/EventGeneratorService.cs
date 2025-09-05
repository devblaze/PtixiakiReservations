using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;

namespace PtixiakiReservations.Services;

public interface IEventGeneratorService
{
    Task<EventGenerationResult> GenerateEventsAsync(EventGenerationOptions options);
    Task<bool> CleanupGeneratedDataAsync();
}

public class EventGeneratorService : IEventGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly Random _random;

    // Realistic venue types and names
    private readonly string[] _venueTypes = { "Theater", "Concert Hall", "Arena", "Stadium", "Auditorium", "Opera House", "Club", "Center" };
    private readonly string[] _venueNames = { "Grand", "Royal", "Metropolitan", "Central", "Apollo", "Phoenix", "Crystal", "Golden", "Silver", "Diamond", "Palace", "Empire", "Victoria" };
    
    // Event types
    private readonly string[] _eventNames = {
        "Rock Concert", "Jazz Night", "Classical Symphony", "Pop Festival", "Comedy Show", 
        "Theater Play", "Musical", "Opera", "Dance Performance", "Stand-up Comedy",
        "Art Exhibition", "Film Screening", "Poetry Reading", "Book Launch", "Tech Conference"
    };

    // Greek cities
    private readonly string[] _greekCities = {
        "Athens", "Thessaloniki", "Patras", "Heraklion", "Larissa", "Volos", "Ioannina", 
        "Kavala", "Chania", "Kalamata", "Rhodes", "Agrinio", "Serres", "Katerini", "Veroia"
    };

    public EventGeneratorService(ApplicationDbContext context)
    {
        _context = context;
        _random = new Random();
    }

    public async Task<EventGenerationResult> GenerateEventsAsync(EventGenerationOptions options)
    {
        var result = new EventGenerationResult();

        try
        {
            // Ensure we have cities and event types
            await EnsureBasicDataExistsAsync();

            var userId = await GetOrCreateTestUserAsync();
            var cities = await _context.City.ToListAsync();
            var eventTypes = await _context.EventType.ToListAsync();

            // Generate venues
            for (int i = 0; i < options.VenueCount; i++)
            {
                var venue = await GenerateVenueAsync(userId, cities);
                result.GeneratedVenues.Add(venue);

                // Generate subareas for this venue
                var subAreaCount = _random.Next(options.MinSubAreasPerVenue, options.MaxSubAreasPerVenue + 1);
                for (int j = 0; j < subAreaCount; j++)
                {
                    var subArea = await GenerateSubAreaAsync(venue.Id);
                    result.GeneratedSubAreas.Add(subArea);

                    // Generate seats for this subarea
                    if (options.GenerateSeats)
                    {
                        var seats = await GenerateSeatsAsync(subArea, options.MinSeatsPerSubArea, options.MaxSeatsPerSubArea);
                        result.GeneratedSeats.AddRange(seats);
                    }
                }

                // Generate events for this venue only if it has SubAreas
                var venueSubAreas = result.GeneratedSubAreas.Where(sa => sa.VenueId == venue.Id).ToList();

                if (venueSubAreas.Any())
                {
                    var eventCount = _random.Next(options.MinEventsPerVenue, options.MaxEventsPerVenue + 1);
                    for (int k = 0; k < eventCount; k++)
                    {
                        var eventItem = await GenerateEventAsync(venue, venueSubAreas, eventTypes, options);
                        result.GeneratedEvents.Add(eventItem);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private async Task EnsureBasicDataExistsAsync()
    {
        // Ensure cities exist
        var existingCitiesCount = await _context.City.CountAsync();
        if (existingCitiesCount == 0)
        {
            var cities = _greekCities.Select(name => new City { Name = name }).ToList();
            await _context.City.AddRangeAsync(cities);
            await _context.SaveChangesAsync();
        }

        // Ensure event types exist
        var existingEventTypesCount = await _context.EventType.CountAsync();
        if (existingEventTypesCount == 0)
        {
            var eventTypes = new[]
            {
                "Concert", "Theater", "Comedy", "Musical", "Opera", "Dance", 
                "Conference", "Exhibition", "Sports", "Festival", "Workshop"
            }.Select(name => new EventType { Name = name }).ToList();

            await _context.EventType.AddRangeAsync(eventTypes);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<string> GetOrCreateTestUserAsync()
    {
        const string testUserId = "event-generator-user";
        
        var existingUser = await _context.Users.FindAsync(testUserId);
        if (existingUser == null)
        {
            var user = new ApplicationUser
            {
                Id = testUserId,
                UserName = "EventGenerator",
                Email = "eventgenerator@example.com",
                EmailConfirmed = true,
                FirstName = "Event",
                LastName = "Generator",
                Address = "Athens, Greece"
            };
            
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        return testUserId;
    }

    private async Task<Venue> GenerateVenueAsync(string userId, List<City> cities)
    {
        var venueName = $"{_venueNames[_random.Next(_venueNames.Length)]} {_venueTypes[_random.Next(_venueTypes.Length)]}";
        var city = cities[_random.Next(cities.Count)];
        
        var venue = new Venue
        {
            Name = venueName,
            Address = $"{_random.Next(1, 999)} {GenerateStreetName()} Street",
            CityId = city.Id,
            PostalCode = GeneratePostalCode(),
            Phone = GeneratePhoneNumber(),
            UserId = userId,
            imgUrl = GenerateVenueImageUrl()
        };

        await _context.Venue.AddAsync(venue);
        return venue;
    }

    private async Task<SubArea> GenerateSubAreaAsync(int venueId)
    {
        var areaNames = new[] { "Orchestra", "Balcony", "Mezzanine", "Gallery", "Box Seats", "VIP Section", "General Admission", "Standing Area" };
        var areaName = areaNames[_random.Next(areaNames.Length)];
        
        // Generate realistic dimensions and positions
        var width = _random.Next(100, 500);
        var height = _random.Next(50, 300);
        
        var subArea = new SubArea
        {
            AreaName = areaName,
            Width = width,
            Height = height,
            Top = _random.Next(0, 200),
            Left = _random.Next(0, 200),
            Rotate = _random.Next(0, 360),
            Desc = $"{areaName} seating area with capacity for approximately {_random.Next(50, 500)} people",
            VenueId = venueId
        };

        await _context.SubArea.AddAsync(subArea);
        return subArea;
    }

    private async Task<List<Seat>> GenerateSeatsAsync(SubArea subArea, int minSeats, int maxSeats)
    {
        var seats = new List<Seat>();
        var seatCount = _random.Next(minSeats, maxSeats + 1);
        
        // Calculate rows and seats per row for a realistic layout
        var rowCount = (int)Math.Ceiling(Math.Sqrt(seatCount));
        var seatsPerRow = (int)Math.Ceiling((double)seatCount / rowCount);
        
        var seatCounter = 0;
        for (int row = 1; row <= rowCount && seatCounter < seatCount; row++)
        {
            for (int seatNum = 1; seatNum <= seatsPerRow && seatCounter < seatCount; seatNum++)
            {
                var seat = new Seat
                {
                    Name = $"{(char)('A' + row - 1)}{seatNum:D2}",
                    X = (decimal)(seatNum * (subArea.Width / seatsPerRow)),
                    Y = (decimal)(row * (subArea.Height / rowCount)),
                    Available = _random.NextDouble() > 0.1, // 90% available, 10% unavailable
                    SubAreaId = subArea.Id
                };
                
                seats.Add(seat);
                seatCounter++;
            }
        }

        await _context.Seat.AddRangeAsync(seats);
        return seats;
    }

    private async Task<Event> GenerateEventAsync(Venue venue, List<SubArea> venueSubAreas, List<EventType> eventTypes, EventGenerationOptions options)
    {
        var eventType = eventTypes[_random.Next(eventTypes.Count)];
        var eventName = _eventNames[_random.Next(_eventNames.Length)];
        
        // Generate future dates within the specified range
        var startDate = DateTime.Now.AddDays(_random.Next(options.MinDaysInFuture, options.MaxDaysInFuture + 1));
        
        // Add random time of day
        var startTime = TimeSpan.FromHours(_random.Next(10, 23)) + TimeSpan.FromMinutes(_random.Next(0, 60));
        startDate = startDate.Date + startTime;
        
        // Event duration between 1-4 hours
        var duration = TimeSpan.FromHours(_random.NextDouble() * 3 + 1);
        var endDate = startDate + duration;
        
        // Always assign a SubArea if available (which should always be the case now)
        int? subAreaId = null;
        if (venueSubAreas.Any())
        {
            subAreaId = venueSubAreas[_random.Next(venueSubAreas.Count)].Id;
        }

        var eventItem = new Event
        {
            Name = $"{eventName} - {venue.Name}",
            StartDateTime = startDate,
            EndTime = endDate,
            EventTypeId = eventType.Id,
            VenueId = venue.Id,
            SubAreaId = subAreaId
        };

        await _context.Event.AddAsync(eventItem);
        return eventItem;
    }

    public async Task<bool> CleanupGeneratedDataAsync()
    {
        try
        {
            // Remove generated events
            var generatedEvents = await _context.Event
                .Where(e => e.Name.Contains("Generator") || e.Venue.UserId == "event-generator-user")
                .ToListAsync();
            _context.Event.RemoveRange(generatedEvents);

            // Remove generated seats
            var generatedSeats = await _context.Seat
                .Where(s => s.SubArea.Venue.UserId == "event-generator-user")
                .ToListAsync();
            _context.Seat.RemoveRange(generatedSeats);

            // Remove generated subareas
            var generatedSubAreas = await _context.SubArea
                .Where(sa => sa.Venue.UserId == "event-generator-user")
                .ToListAsync();
            _context.SubArea.RemoveRange(generatedSubAreas);

            // Remove generated venues
            var generatedVenues = await _context.Venue
                .Where(v => v.UserId == "event-generator-user")
                .ToListAsync();
            _context.Venue.RemoveRange(generatedVenues);

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GenerateStreetName()
    {
        var streetNames = new[] { "Panepistimiou", "Ermou", "Stadiou", "Patission", "Kifissias", "Vouliagmenis", "Syngrou", "Alexandras" };
        return streetNames[_random.Next(streetNames.Length)];
    }

    private string GeneratePostalCode()
    {
        return _random.Next(10000, 99999).ToString();
    }

    private string GeneratePhoneNumber()
    {
        return $"210-{_random.Next(1000000, 9999999)}";
    }

    private string GenerateVenueImageUrl()
    {
        var images = new[]
        {
            "https://images.unsplash.com/photo-1540039155733-5bb30b53aa14?w=800",
            "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800",
            "https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?w=800",
            "https://images.unsplash.com/photo-1514306191717-452ec28c7814?w=800"
        };
        return images[_random.Next(images.Length)];
    }
}

public class EventGenerationOptions
{
    public int VenueCount { get; set; } = 5;
    public int MinSubAreasPerVenue { get; set; } = 2;
    public int MaxSubAreasPerVenue { get; set; } = 5;
    public int MinEventsPerVenue { get; set; } = 3;
    public int MaxEventsPerVenue { get; set; } = 8;
    public bool GenerateSeats { get; set; } = true;
    public int MinSeatsPerSubArea { get; set; } = 20;
    public int MaxSeatsPerSubArea { get; set; } = 100;
    public int MinDaysInFuture { get; set; } = 1;
    public int MaxDaysInFuture { get; set; } = 90;
}

public class EventGenerationResult
{
    public bool Success { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<Venue> GeneratedVenues { get; set; } = new();
    public List<SubArea> GeneratedSubAreas { get; set; } = new();
    public List<Seat> GeneratedSeats { get; set; } = new();
    public List<Event> GeneratedEvents { get; set; } = new();
    
    public int TotalItemsGenerated => GeneratedVenues.Count + GeneratedSubAreas.Count + GeneratedSeats.Count + GeneratedEvents.Count;
}