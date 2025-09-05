using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Services;
using Xunit;

namespace PtixiakiReservations.Tests.Services;

public class EventGeneratorServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventGeneratorService _service;

    public EventGeneratorServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new EventGeneratorService(_context);

        SeedBasicData();
    }

    private void SeedBasicData()
    {
        // Add cities
        _context.City.AddRange(
            new City { Id = 1, Name = "Athens" },
            new City { Id = 2, Name = "Thessaloniki" }
        );

        // Add event types
        _context.EventType.AddRange(
            new EventType { Id = 1, Name = "Concert" },
            new EventType { Id = 2, Name = "Theater" }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task GenerateEventsAsync_Should_Only_Create_Events_For_Venues_With_SubAreas()
    {
        // Arrange
        var options = new EventGenerationOptions
        {
            VenueCount = 2,
            MinSubAreasPerVenue = 0,
            MaxSubAreasPerVenue = 2,
            MinEventsPerVenue = 2,
            MaxEventsPerVenue = 3,
            GenerateSeats = false
        };

        // Act
        var result = await _service.GenerateEventsAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.GeneratedVenues.Count);

        // Only venues with SubAreas should have events
        foreach (var venue in result.GeneratedVenues)
        {
            var venueSubAreas = result.GeneratedSubAreas.Where(sa => sa.VenueId == venue.Id).ToList();
            var venueEvents = result.GeneratedEvents.Where(e => e.VenueId == venue.Id).ToList();

            if (venueSubAreas.Any())
            {
                Assert.NotEmpty(venueEvents);
                // All events should have SubAreaId assigned
                foreach (var evt in venueEvents)
                {
                    Assert.NotNull(evt.SubAreaId);
                    Assert.Contains(evt.SubAreaId.Value, venueSubAreas.Select(sa => sa.Id));
                }
            }
            else
            {
                Assert.Empty(venueEvents);
            }
        }
    }

    [Fact]
    public async Task GenerateEventsAsync_Should_Assign_SubAreaId_To_All_Events()
    {
        // Arrange
        var options = new EventGenerationOptions
        {
            VenueCount = 1,
            MinSubAreasPerVenue = 2,
            MaxSubAreasPerVenue = 3,
            MinEventsPerVenue = 5,
            MaxEventsPerVenue = 5,
            GenerateSeats = true,
            MinSeatsPerSubArea = 10,
            MaxSeatsPerSubArea = 20
        };

        // Act
        var result = await _service.GenerateEventsAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.GeneratedVenues);
        Assert.True(result.GeneratedSubAreas.Count >= 2);
        Assert.Equal(5, result.GeneratedEvents.Count);

        // All events should have SubAreaId
        foreach (var evt in result.GeneratedEvents)
        {
            Assert.NotNull(evt.SubAreaId);
            var subArea = result.GeneratedSubAreas.FirstOrDefault(sa => sa.Id == evt.SubAreaId);
            Assert.NotNull(subArea);
            Assert.Equal(evt.VenueId, subArea.VenueId);
        }
    }

    [Fact]
    public async Task GenerateEventsAsync_Should_Generate_Seats_With_Correct_SubAreaId()
    {
        // Arrange
        var options = new EventGenerationOptions
        {
            VenueCount = 1,
            MinSubAreasPerVenue = 1,
            MaxSubAreasPerVenue = 1,
            MinEventsPerVenue = 1,
            MaxEventsPerVenue = 1,
            GenerateSeats = true,
            MinSeatsPerSubArea = 5,
            MaxSeatsPerSubArea = 10
        };

        // Act
        var result = await _service.GenerateEventsAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.GeneratedSubAreas);
        Assert.True(result.GeneratedSeats.Count >= 5);

        var subArea = result.GeneratedSubAreas.First();
        foreach (var seat in result.GeneratedSeats)
        {
            Assert.Equal(subArea.Id, seat.SubAreaId);
        }
    }

    [Fact]
    public async Task GenerateEventsAsync_Should_Create_Events_With_Valid_DateTimes()
    {
        // Arrange
        var options = new EventGenerationOptions
        {
            VenueCount = 1,
            MinSubAreasPerVenue = 1,
            MaxSubAreasPerVenue = 1,
            MinEventsPerVenue = 3,
            MaxEventsPerVenue = 3,
            GenerateSeats = false
        };

        // Act
        var result = await _service.GenerateEventsAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.GeneratedEvents.Count);

        foreach (var evt in result.GeneratedEvents)
        {
            Assert.True(evt.StartDateTime > DateTime.Now);
            Assert.True(evt.EndTime > evt.StartDateTime);
            Assert.True((evt.EndTime - evt.StartDateTime).TotalHours >= 1);
            Assert.True((evt.EndTime - evt.StartDateTime).TotalHours <= 4);
        }
    }

    [Fact]
    public async Task CleanupGeneratedDataAsync_Should_Remove_Generated_Data()
    {
        // Arrange
        var options = new EventGenerationOptions
        {
            VenueCount = 1,
            MinSubAreasPerVenue = 1,
            MaxSubAreasPerVenue = 1,
            MinEventsPerVenue = 1,
            MaxEventsPerVenue = 1,
            GenerateSeats = true,
            MinSeatsPerSubArea = 5,
            MaxSeatsPerSubArea = 5
        };

        await _service.GenerateEventsAsync(options);
        await _context.SaveChangesAsync();

        // Act
        var cleanupResult = await _service.CleanupGeneratedDataAsync();

        // Assert
        Assert.True(cleanupResult);

        var remainingEvents = await _context.Event.Where(e => e.Name.Contains("Generator")).CountAsync();
        var remainingSeats = await _context.Seat.CountAsync();
        var remainingSubAreas = await _context.SubArea.CountAsync();
        var remainingVenues = await _context.Venue.Where(v => v.UserId == "event-generator-user").CountAsync();

        Assert.Equal(0, remainingEvents);
        Assert.Equal(0, remainingSeats);
        Assert.Equal(0, remainingSubAreas);
        Assert.Equal(0, remainingVenues);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}