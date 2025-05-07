using Moq;
using Microsoft.AspNetCore.Mvc;
using PtixiakiReservations.Controllers;
using PtixiakiReservations.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PtixiakiReservations.Data;
using PtixiakiReservations.Services;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.Tests.Controllers;

public class EventsControllerTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<IElasticSearch> _elasticSearchMock;
    private readonly Mock<ILogger<EventsController>> _loggerMock;
    private readonly ApplicationDbContext _context;

    public EventsControllerTests()
    {
        // Mock dependencies
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();
        _elasticSearchMock = new Mock<IElasticSearch>();
        _loggerMock = new Mock<ILogger<EventsController>>();

        // Create a unique database instance for each test
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        SeedDatabase(_context);
    }

    [Fact]
    public async Task EventsForToday_Should_Return_Future_Events()
    {
        // Arrange
        // Clear existing data first to ensure a clean test environment
        _context.Event.RemoveRange(_context.Event);
        await _context.SaveChangesAsync();

        // Setup test dates
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var nextWeek = today.AddDays(7);
        var nextMonth = today.AddDays(30);

        // Add only upcoming events
        var events = new List<Event>
        {
            new Event
            {
                Id = 3,
                Name = "Tomorrow Event",
                StartDateTime = tomorrow.AddHours(12),
                EndTime = tomorrow.AddHours(14),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 4,
                Name = "Next Week Event",
                StartDateTime = nextWeek.AddHours(15),
                EndTime = nextWeek.AddHours(17),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 5,
                Name = "Next Month Event",
                StartDateTime = nextMonth.AddHours(10),
                EndTime = nextMonth.AddHours(12),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            }
        };

        await _context.Event.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.EventsForToday(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Event>>(viewResult.Model);

        // Verify all future events are returned
        Assert.Equal(3, model.Count);

        // Verify all are classified as "upcoming"
        var upcomingEvents = model.Count(e => controller.GetEventTimeClass(e.StartDateTime) == "event-upcoming");
        Assert.Equal(3, upcomingEvents);

        // Check event names are in the expected order
        Assert.Equal("Tomorrow Event", model[0].Name);
        Assert.Equal("Next Week Event", model[1].Name);
        Assert.Equal("Next Month Event", model[2].Name);
    }
    
    [Fact]
    public async Task Details_ReturnsViewResult_WhenEventExists()
    {
        // Arrange
        // Mock the controller's behavior for retrieving the event
        var testEvent = new Event
        {
            Id = 1,
            Name = "Test Event",
            StartDateTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(2),
            VenueId = 1,
            FamilyEventId = 1
        };

        // Create a controller that uses our mocked context
        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Event>(viewResult.Model);
        Assert.Equal(1, model.Id);
        Assert.Equal("Test Event", model.Name);
    }

    [Fact]
    public async Task Details_ReturnsNotFoundResult_WhenEventDoesNotExist()
    {
        // Arrange
        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.Details(-1); // Non-existent ID

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EventsForToday_Returns_Events_WithCorrectDateClassification()
    {
        // Arrange
        // Clear existing data first to ensure a clean test environment
        _context.Event.RemoveRange(_context.Event);
        await _context.SaveChangesAsync();

        // Setup test dates
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        var tomorrow = today.AddDays(1);
        var nextWeek = today.AddDays(7);

        // Add test events with different dates
        var events = new List<Event>
        {
            // Today's events
            new Event
            {
                Id = 1,
                Name = "Morning Event Today",
                StartDateTime = today.AddHours(9),
                EndTime = today.AddHours(11),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 2,
                Name = "Evening Event Today",
                StartDateTime = today.AddHours(18),
                EndTime = today.AddHours(20),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            },

            // Upcoming events
            new Event
            {
                Id = 3,
                Name = "Tomorrow Event",
                StartDateTime = tomorrow.AddHours(12),
                EndTime = tomorrow.AddHours(14),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 4,
                Name = "Next Week Event",
                StartDateTime = nextWeek.AddHours(15),
                EndTime = nextWeek.AddHours(17),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            },

            // Past events
            new Event
            {
                Id = 5,
                Name = "Yesterday Event",
                StartDateTime = yesterday.AddHours(12),
                EndTime = yesterday.AddHours(14),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = _context.Venue.Find(1)
            }
        };

        await _context.Event.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.EventsForToday(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Event>>(viewResult.Model);

        // Verify all events are returned (5 total)
        Assert.Equal(5, model.Count);

        // Verify events are ordered by StartDateTime
        Assert.Equal("Yesterday Event", model[0].Name);
        Assert.Equal("Morning Event Today", model[1].Name);
        Assert.Equal("Evening Event Today", model[2].Name);
        Assert.Equal("Tomorrow Event", model[3].Name);
        Assert.Equal("Next Week Event", model[4].Name);

        // Test the GetEventTimeClass helper method used by the view
        var todayEvents = model.Count(e => IsToday(e.StartDateTime.Date));
        var upcomingEvents = model.Count(e => IsFuture(e.StartDateTime.Date));
        var pastEvents = model.Count(e => IsPast(e.StartDateTime.Date));

        // Verify correct distribution
        Assert.Equal(2, todayEvents);
        Assert.Equal(2, upcomingEvents);
        Assert.Equal(1, pastEvents);
    }

    [Fact]
    public async Task EventsForToday_Filters_By_City_When_Provided()
    {
        // Arrange
        // Clear existing data
        _context.Event.RemoveRange(_context.Event);
        await _context.SaveChangesAsync();

        // Add two cities
        var city1 = new City { Id = 10, Name = "Athens" };
        var city2 = new City { Id = 11, Name = "Thessaloniki" };
        await _context.City.AddRangeAsync(new[] { city1, city2 });
        await _context.SaveChangesAsync();

        // Add venues for each city
        var venue1 = new Venue
        {
            Id = 10,
            Name = "Athens Venue",
            UserId = "user1",
            CityId = 10,
            City = city1
        };
        var venue2 = new Venue
        {
            Id = 11,
            Name = "Thessaloniki Venue",
            UserId = "user1",
            CityId = 11,
            City = city2
        };
        await _context.Venue.AddRangeAsync(new[] { venue1, venue2 });
        await _context.SaveChangesAsync();

        // Add events in both cities
        var today = DateTime.Today;
        var events = new List<Event>
        {
            new Event
            {
                Id = 10,
                Name = "Athens Event 1",
                StartDateTime = today,
                EndTime = today.AddHours(2),
                VenueId = 10,
                FamilyEventId = 1,
                Venue = venue1
            },
            new Event
            {
                Id = 11,
                Name = "Athens Event 2",
                StartDateTime = today.AddDays(1),
                EndTime = today.AddDays(1).AddHours(2),
                VenueId = 10,
                FamilyEventId = 1,
                Venue = venue1
            },
            new Event
            {
                Id = 12,
                Name = "Thessaloniki Event",
                StartDateTime = today,
                EndTime = today.AddHours(2),
                VenueId = 11,
                FamilyEventId = 1,
                Venue = venue2
            }
        };
        await _context.Event.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act - Filter by Athens
        var result = await controller.EventsForToday("Athens");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Event>>(viewResult.Model);

        // Verify only Athens events are returned
        Assert.Equal(2, model.Count);
        Assert.All(model, e => Assert.Equal("Athens", e.Venue.City.Name));

        // Act - Filter by Thessaloniki
        result = await controller.EventsForToday("Thessaloniki");

        // Assert
        viewResult = Assert.IsType<ViewResult>(result);
        model = Assert.IsType<List<Event>>(viewResult.Model);

        // Verify only Thessaloniki events are returned
        Assert.Single(model);
        Assert.Equal("Thessaloniki Event", model[0].Name);
    }

    [Fact]
    public async Task EventsForToday_Handles_Pagination_Correctly()
    {
        // Arrange
        // Clear existing data
        _context.Event.RemoveRange(_context.Event);
        await _context.SaveChangesAsync();

        // Add 25 events
        var events = new List<Event>();
        var venue = _context.Venue.Find(1);

        for (int i = 1; i <= 25; i++)
        {
            events.Add(new Event
            {
                Id = 100 + i,
                Name = $"Event {i}",
                StartDateTime = DateTime.Today.AddHours(i),
                EndTime = DateTime.Today.AddHours(i + 1),
                VenueId = 1,
                FamilyEventId = 1,
                Venue = venue
            });
        }

        await _context.Event.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act - Page 1 with 10 items per page
        var result = await controller.EventsForToday(null, 1, 10);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Event>>(viewResult.Model);

        // Verify correct page size
        Assert.Equal(10, model.Count);
        Assert.Equal("Event 1", model[0].Name);
        Assert.Equal("Event 10", model[9].Name);

        // Act - Page 2 with 10 items per page
        result = await controller.EventsForToday(null, 2, 10);

        // Assert
        viewResult = Assert.IsType<ViewResult>(result);
        model = Assert.IsType<List<Event>>(viewResult.Model);

        // Verify correct page size and content
        Assert.Equal(10, model.Count);
        Assert.Equal("Event 11", model[0].Name);
        Assert.Equal("Event 20", model[9].Name);

        // Act - Page 3 with 10 items per page
        result = await controller.EventsForToday(null, 3, 10);

        // Assert
        viewResult = Assert.IsType<ViewResult>(result);
        model = Assert.IsType<List<Event>>(viewResult.Model);

        // Verify correct page size and content
        Assert.Equal(5, model.Count); // Only 5 items left for page 3
        Assert.Equal("Event 21", model[0].Name);
        Assert.Equal("Event 25", model[4].Name);
    }

// Helper methods for date classification
    private bool IsToday(DateTime date)
    {
        return date.Date == DateTime.Today;
    }

    private bool IsFuture(DateTime date)
    {
        return date.Date > DateTime.Today;
    }

    private bool IsPast(DateTime date)
    {
        return date.Date < DateTime.Today;
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        return userManagerMock;
    }

    private static Mock<RoleManager<ApplicationRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            store.Object,
            null,
            null,
            null,
            null);

        return roleManagerMock;
    }

    private static void SeedDatabase(ApplicationDbContext context)
    {
        // Clear existing data
        context.Database.EnsureDeleted();

        // Add a FamilyEvent
        var familyEvent = new FamilyEvent
        {
            Id = 1,
            Name = "Test Family Event"
        };
        context.Add(familyEvent);

        // Add a user
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser@example.com",
            Email = "testuser@example.com"
        };
        context.Add(user);

        // Add a city
        var city = new City
        {
            Id = 1,
            Name = "Test City"
        };
        context.Add(city);

        // Add a venue
        var venue = new Venue
        {
            Id = 1,
            Name = "Test Venue",
            UserId = "user1",
            CityId = 1,
            ApplicationUser = user,
            City = city
        };
        context.Add(venue);

        // Add an event type
        var eventType = new EventType
        {
            Id = 1,
            Name = "Test Event Type"
        };
        context.Add(eventType);

        // Add an event with all required relationships
        var testEvent = new Event
        {
            Id = 1,
            Name = "Test Event",
            StartDateTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(2),
            EventTypeId = 1,
            VenueId = 1,
            FamilyEventId = 1,
            Venue = venue,
            FamilyEvent = familyEvent,
            EventType = eventType
        };
        context.Add(testEvent);

        context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}