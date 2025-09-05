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
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using System.Linq;

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

    #region Event Creation Tests

    [Fact]
    public async Task CreateEvent_GET_ReturnsView_WithDropdownData()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        SetupHttpContext(controller, "user1");

        // Act
        var result = await controller.CreateEvent();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.ViewData["VenueList"]);
        Assert.NotNull(viewResult.ViewData["EventTypeList"]);
    }

    [Fact]
    public async Task CreateEvent_GET_RedirectsToVenueCreate_WhenUserHasNoVenues()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user2");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        SetupHttpContext(controller, "user2");

        // Act
        var result = await controller.CreateEvent();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Create", redirectResult.ActionName);
        Assert.Equal("Venue", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CreateEvent_SingleDay_CreatesEventSuccessfully()
    {
        // Arrange
        var newEvent = new Event
        {
            Name = "Single Day Test Event",
            VenueId = 1,
            EventTypeId = 1,
            StartDateTime = DateTime.Today.AddDays(1).AddHours(10),
            EndTime = DateTime.Today.AddDays(1).AddHours(12)
        };

        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Clear existing events
        _context.Event.RemoveRange(_context.Event.Where(e => e.Id != 1));
        await _context.SaveChangesAsync();

        // Act
        var result = await controller.CreateEvent(newEvent);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        // Verify event was created
        var createdEvent = await _context.Event
            .FirstOrDefaultAsync(e => e.Name == "Single Day Test Event");
        Assert.NotNull(createdEvent);
        Assert.Equal("Single Day Test Event", createdEvent.Name);
        Assert.Equal(1, createdEvent.VenueId);
        Assert.Equal(1, createdEvent.EventTypeId);
    }

    [Fact]
    public async Task CreateEvent_MultiDay_WithTimeString_CreatesMultipleEvents()
    {
        // Arrange
        var newEvent = new Event
        {
            Name = "Multi Day Test Event",
            VenueId = 1,
            EventTypeId = 1
        };

        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Clear existing events
        _context.Event.RemoveRange(_context.Event.Where(e => e.Id != 1));
        await _context.SaveChangesAsync();

        var startDate = DateTime.Today.AddDays(1);
        var endDate = DateTime.Today.AddDays(3);

        // Act - Test with time-only strings (like "10:00" and "12:00")
        var result = await controller.CreateEvent(
            newEvent,
            "on", // IsMultiDay
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd"),
            "10:00", // StartTime
            "12:00"  // EndTime
        );

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        // Verify 3 events were created (3 days)
        var createdEvents = await _context.Event
            .Where(e => e.Name == "Multi Day Test Event")
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();
        
        Assert.Equal(3, createdEvents.Count);

        // Verify each event has correct date and time
        for (int i = 0; i < 3; i++)
        {
            var expectedDate = startDate.AddDays(i);
            Assert.Equal(expectedDate.Date, createdEvents[i].StartDateTime.Date);
            Assert.Equal(10, createdEvents[i].StartDateTime.Hour);
            Assert.Equal(0, createdEvents[i].StartDateTime.Minute);
            Assert.Equal(12, createdEvents[i].EndTime.Hour);
            Assert.Equal(0, createdEvents[i].EndTime.Minute);
        }
    }

    [Fact]
    public async Task CreateEvent_MultiDay_WithDateTimeString_CreatesMultipleEvents()
    {
        // Arrange
        var newEvent = new Event
        {
            Name = "Multi Day DateTime Test Event",
            VenueId = 1,
            EventTypeId = 1
        };

        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Clear existing events
        _context.Event.RemoveRange(_context.Event.Where(e => e.Id != 1));
        await _context.SaveChangesAsync();

        var startDate = DateTime.Today.AddDays(1);
        var endDate = DateTime.Today.AddDays(2);

        // Act - Test with datetime strings (like "2025-09-04T14:30")
        var result = await controller.CreateEvent(
            newEvent,
            "true", // IsMultiDay
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd"),
            "2025-01-01T14:30", // StartTime - date part will be ignored, only time extracted
            "2025-01-01T16:30"  // EndTime - date part will be ignored, only time extracted
        );

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        // Verify 2 events were created (2 days)
        var createdEvents = await _context.Event
            .Where(e => e.Name == "Multi Day DateTime Test Event")
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();
        
        Assert.Equal(2, createdEvents.Count);

        // Verify each event has correct date and time (time extracted from datetime string)
        for (int i = 0; i < 2; i++)
        {
            var expectedDate = startDate.AddDays(i);
            Assert.Equal(expectedDate.Date, createdEvents[i].StartDateTime.Date);
            Assert.Equal(14, createdEvents[i].StartDateTime.Hour); // Time extracted from datetime string
            Assert.Equal(30, createdEvents[i].StartDateTime.Minute);
            Assert.Equal(16, createdEvents[i].EndTime.Hour);
            Assert.Equal(30, createdEvents[i].EndTime.Minute);
        }
    }

    [Fact]
    public async Task CreateEvent_MultiDay_InvalidVenue_ReturnsViewWithError()
    {
        // Arrange
        var newEvent = new Event
        {
            Name = "Test Event",
            VenueId = 999, // Non-existent venue
            EventTypeId = 1
        };

        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.CreateEvent(newEvent, "on", "2024-01-01", "2024-01-03", "10:00", "12:00");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("VenueId"));
    }

    [Fact]
    public async Task CreateEvent_UserDoesNotOwnVenue_ReturnsViewWithError()
    {
        // Arrange
        var newEvent = new Event
        {
            Name = "Test Event",
            VenueId = 1,
            EventTypeId = 1
        };

        // Set up user that doesn't own the venue
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user2");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.CreateEvent(newEvent, "on", "2024-01-01", "2024-01-03", "10:00", "12:00");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("VenueId"));
    }

    #endregion

    #region Event Search and Filtering Tests

    [Fact]
    public async Task SearchEvents_WithDateRange_ReturnsFilteredEvents()
    {
        // Arrange
        _context.Event.RemoveRange(_context.Event);
        await _context.SaveChangesAsync();

        var events = new List<Event>
        {
            new Event
            {
                Id = 10, Name = "Event 1", StartDateTime = new DateTime(2024, 6, 1, 10, 0, 0),
                EndTime = new DateTime(2024, 6, 1, 12, 0, 0), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 11, Name = "Event 2", StartDateTime = new DateTime(2024, 6, 15, 14, 0, 0),
                EndTime = new DateTime(2024, 6, 15, 16, 0, 0), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 12, Name = "Event 3", StartDateTime = new DateTime(2024, 7, 1, 18, 0, 0),
                EndTime = new DateTime(2024, 7, 1, 20, 0, 0), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
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
        var result = await controller.SearchEvents(
            eventTypeId: null,
            startDate: "2024-06-01",
            endDate: "2024-06-30",
            searchTerm: null
        );

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        dynamic data = jsonResult.Value;
        var eventsList = (List<Event>)data.GetType().GetProperty("events").GetValue(data);
        
        Assert.Equal(2, eventsList.Count); // Only June events
        Assert.All(eventsList, e => Assert.True(e.StartDateTime.Month == 6));
    }

    [Fact]
    public async Task SearchEvents_WithSearchTerm_ReturnsMatchingEvents()
    {
        // Arrange
        _context.Event.RemoveRange(_context.Event);
        await _context.SaveChangesAsync();

        var events = new List<Event>
        {
            new Event
            {
                Id = 20, Name = "Concert at Arena", StartDateTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(1).AddHours(2), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 21, Name = "Theater Show", StartDateTime = DateTime.Now.AddDays(2),
                EndTime = DateTime.Now.AddDays(2).AddHours(2), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 22, Name = "Music Concert", StartDateTime = DateTime.Now.AddDays(3),
                EndTime = DateTime.Now.AddDays(3).AddHours(2), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
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
        var result = await controller.SearchEvents(
            eventTypeId: null,
            startDate: null,
            endDate: null,
            searchTerm: "concert"
        );

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        dynamic data = jsonResult.Value;
        var eventsList = (List<Event>)data.GetType().GetProperty("events").GetValue(data);
        
        Assert.Equal(2, eventsList.Count); // Events containing "concert"
        Assert.All(eventsList, e => Assert.Contains("concert", e.Name.ToLower()));
    }

    [Fact]
    public async Task GetTodayEvents_ReturnsCorrectEvents()
    {
        // Arrange
        _context.Event.RemoveRange(_context.Event);
        await _context.SaveChangesAsync();

        var today = DateTime.Today;
        var events = new List<Event>
        {
            new Event
            {
                Id = 30, Name = "Today Event 1", StartDateTime = today.AddHours(10),
                EndTime = today.AddHours(12), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 31, Name = "Today Event 2", StartDateTime = today.AddHours(14),
                EndTime = today.AddHours(16), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
            },
            new Event
            {
                Id = 32, Name = "Tomorrow Event", StartDateTime = today.AddDays(1).AddHours(10),
                EndTime = today.AddDays(1).AddHours(12), VenueId = 1, EventTypeId = 1, Venue = _context.Venue.Find(1)
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
        var result = await controller.GetTodayEvents(null);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        dynamic data = jsonResult.Value;
        var eventsList = (List<Event>)data.GetType().GetProperty("events").GetValue(data);
        
        Assert.Equal(2, eventsList.Count); // Only today's events
        Assert.All(eventsList, e => Assert.Equal(today.Date, e.StartDateTime.Date));
    }

    #endregion

    #region Event Editing Tests

    [Fact]
    public async Task Edit_GET_ReturnsViewWithEvent()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Event>(viewResult.Model);
        Assert.Equal(1, model.Id);
        Assert.Equal("Test Event", model.Name);
        Assert.NotNull(viewResult.ViewData["VenueList"]);
        Assert.NotNull(viewResult.ViewData["EventTypeList"]);
        Assert.NotNull(viewResult.ViewData["SubAreaList"]);
    }

    [Fact]
    public async Task Edit_GET_ReturnsNotFound_WhenEventDoesNotExist()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.Edit(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_GET_ReturnsNotFound_WhenUserDoesNotOwnVenue()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user2");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        SetupHttpContext(controller, "user2");

        // Act
        var result = await controller.Edit(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Edit_POST_UpdatesEventSuccessfully()
    {
        // Arrange
        var updatedEvent = new Event
        {
            Id = 1,
            Name = "Updated Event Name",
            StartDateTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(1).AddHours(3),
            EventTypeId = 1,
            VenueId = 1
        };

        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        SetupHttpContext(controller, "user1");

        // Act
        var result = await controller.Edit(1, updatedEvent);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("VenueEvents", redirectResult.ActionName);

        // Verify the event was updated
        var eventFromDb = await _context.Event.FindAsync(1);
        Assert.Equal("Updated Event Name", eventFromDb.Name);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void GetEventTimeClass_ReturnsCorrectClasses()
    {
        // Arrange
        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);

        // Act & Assert
        Assert.Equal("event-today", controller.GetEventTimeClass(today.AddHours(10)));
        Assert.Equal("event-upcoming", controller.GetEventTimeClass(tomorrow.AddHours(10)));
        Assert.Equal("event-past", controller.GetEventTimeClass(yesterday.AddHours(10)));
    }

    [Fact]
    public async Task GetUserEvents_ReturnsUserOwnedEvents()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object);

        // Act
        var result = await controller.GetUserEvents();

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var events = Assert.IsType<List<Event>>(jsonResult.Value);
        
        // All returned events should belong to venues owned by user1
        Assert.All(events, e => Assert.Equal("user1", e.Venue.UserId));
    }

    #endregion

    #region Helper Methods

    private void SetupHttpContext(EventsController controller, string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var httpContext = new DefaultHttpContext()
        {
            User = user
        };
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        // Mock TempData
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new Mock<ITempDataDictionaryFactory>();
        var tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
        tempDataDictionaryFactory.Setup(x => x.GetTempData(httpContext)).Returns(tempData);
        controller.TempData = tempData;
    }

    #endregion

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

    #region SubAreaId Tests

    [Fact]
    public async Task CreateEvent_SingleEvent_Should_Include_SubAreaId()
    {
        // Arrange
        var controller = CreateController();
        var userId = "user1";  // Use the same userId as seeded venue
        SetupUserManager(userId);

        // Add SubArea to the seeded venue
        var subArea = new SubArea
        {
            Id = 10,
            VenueId = 1,
            AreaName = "Main Hall",
            Width = 100,
            Height = 100
        };
        _context.SubArea.Add(subArea);
        await _context.SaveChangesAsync();

        var newEvent = new Event
        {
            Name = "Test Concert",
            VenueId = 1,
            EventTypeId = 1,
            SubAreaId = 10,
            StartDateTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(1).AddHours(2)
        };

        // Act
        var result = await controller.CreateEvent(newEvent);

        // Assert
        var createdEvent = await _context.Event
            .FirstOrDefaultAsync(e => e.Name == "Test Concert");

        Assert.NotNull(createdEvent);
        Assert.Equal(10, createdEvent.SubAreaId);
        Assert.Equal("Test Concert", createdEvent.Name);
        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task CreateEvent_MultiDay_Should_Include_SubAreaId_For_All_Days()
    {
        // Arrange
        var controller = CreateController();
        var userId = "user1";  // Use the same userId as seeded venue
        SetupUserManager(userId);

        // Add SubArea to the seeded venue
        var subArea = new SubArea
        {
            Id = 20,
            VenueId = 1,
            AreaName = "Theater Section A",
            Width = 150,
            Height = 200
        };
        _context.SubArea.Add(subArea);
        await _context.SaveChangesAsync();

        var newEvent = new Event
        {
            Name = "Multi-Day Festival",
            VenueId = 1,
            EventTypeId = 1,
            SubAreaId = 20
        };

        // Act
        var result = await controller.CreateEvent(
            newEvent,
            IsMultiDay: "true",
            StartDate: DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
            EndDate: DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
            StartTime: "19:00",
            EndTime: "23:00"
        );

        // Assert
        var createdEvents = await _context.Event
            .Where(e => e.Name == "Multi-Day Festival")
            .ToListAsync();

        Assert.Equal(3, createdEvents.Count); // 3 days of events
        foreach (var evt in createdEvents)
        {
            Assert.Equal(20, evt.SubAreaId);
            Assert.Equal("Multi-Day Festival", evt.Name);
        }
        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task CreateEvent_Without_SubAreaId_Should_Still_Create_Event()
    {
        // Arrange
        var controller = CreateController();
        var userId = "user1";  // Use the same userId as seeded venue
        SetupUserManager(userId);

        var newEvent = new Event
        {
            Name = "Event Without SubArea",
            VenueId = 1,
            EventTypeId = 1,
            SubAreaId = null,
            StartDateTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(1).AddHours(2)
        };

        // Act
        var result = await controller.CreateEvent(newEvent);

        // Assert
        var createdEvent = await _context.Event
            .FirstOrDefaultAsync(e => e.Name == "Event Without SubArea");

        Assert.NotNull(createdEvent);
        Assert.Null(createdEvent.SubAreaId);
        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task GetSubAreas_Should_Return_SubAreas_For_Venue()
    {
        // Arrange
        var controller = CreateController();

        // Add multiple SubAreas
        var subAreas = new List<SubArea>
        {
            new SubArea { Id = 30, VenueId = 1, AreaName = "Section A" },
            new SubArea { Id = 31, VenueId = 1, AreaName = "Section B" },
            new SubArea { Id = 32, VenueId = 2, AreaName = "Other Venue Section" }
        };
        _context.SubArea.AddRange(subAreas);
        await _context.SaveChangesAsync();

        // Act
        var result = controller.GetSubAreas(1);

        // Assert
        Assert.IsType<JsonResult>(result);
        var jsonResult = result as JsonResult;
        var data = jsonResult.Value as IEnumerable<dynamic>;

        Assert.NotNull(data);
        var subAreaList = data.ToList();
        Assert.Equal(2, subAreaList.Count); // Only venue 1 subareas
    }

    #endregion

    private EventsController CreateController()
    {
        var controller = new EventsController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object,
            _loggerMock.Object
        );

        // Setup HttpContext with TempData
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private void SetupUserManager(string userId)
    {
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, UserName = "test@test.com" });
    }
}