using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PtixiakiReservations.Controllers;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using Xunit;

namespace PtixiakiReservations.Tests.Controllers;

public class ReservationControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly ReservationController _controller;

    public ReservationControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();

        _controller = new ReservationController(
            _context,
            _userManagerMock.Object,
            _roleManagerMock.Object
        );

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add city
        var city = new City { Id = 1, Name = "Test City" };
        _context.City.Add(city);

        // Add venue
        var venue = new Venue
        {
            Id = 1,
            Name = "Test Venue",
            UserId = "user1",
            CityId = 1
        };
        _context.Venue.Add(venue);

        // Add event type
        var eventType = new EventType
        {
            Id = 1,
            Name = "Concert"
        };
        _context.EventType.Add(eventType);

        // Add sub areas
        var subArea1 = new SubArea
        {
            Id = 1,
            VenueId = 1,
            AreaName = "Main Hall",
            Width = 100,
            Height = 100
        };
        var subArea2 = new SubArea
        {
            Id = 2,
            VenueId = 1,
            AreaName = "Balcony",
            Width = 80,
            Height = 60
        };
        _context.SubArea.AddRange(subArea1, subArea2);

        _context.SaveChanges();
    }

    [Fact]
    public async Task ReserveSeats_WithValidEventId_And_SubAreaId_Should_Return_View()
    {
        // Arrange
        var eventWithSubArea = new Event
        {
            Id = 1,
            Name = "Concert with SubArea",
            VenueId = 1,
            EventTypeId = 1,
            SubAreaId = 1,
            StartDateTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(1).AddHours(2)
        };
        _context.Event.Add(eventWithSubArea);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ReserveSeats(1);

        // Assert
        Assert.IsType<ViewResult>(result);
        var viewResult = result as ViewResult;
        var model = viewResult.Model as Event;

        Assert.NotNull(model);
        Assert.Equal(1, model.SubAreaId);
        Assert.Equal("Concert with SubArea", model.Name);
        Assert.Equal(1, viewResult.ViewData["EventId"]);
        Assert.Equal(1, viewResult.ViewData["VenueId"]);
    }

    [Fact]
    public async Task ReserveSeats_WithValidEventId_Without_SubAreaId_Should_Return_View_With_Null_SubArea()
    {
        // Arrange
        var eventWithoutSubArea = new Event
        {
            Id = 2,
            Name = "Concert without SubArea",
            VenueId = 1,
            EventTypeId = 1,
            SubAreaId = null,
            StartDateTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(1).AddHours(2)
        };
        _context.Event.Add(eventWithoutSubArea);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ReserveSeats(2);

        // Assert
        Assert.IsType<ViewResult>(result);
        var viewResult = result as ViewResult;
        var model = viewResult.Model as Event;

        Assert.NotNull(model);
        Assert.Null(model.SubAreaId);
        Assert.Null(model.SubArea);
        Assert.Equal("Concert without SubArea", model.Name);
    }

    [Fact]
    public async Task ReserveSeats_With_Null_EventId_Should_Return_NotFound()
    {
        // Act
        var result = await _controller.ReserveSeats(null);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ReserveSeats_With_NonExistent_EventId_Should_Return_NotFound()
    {
        // Act
        var result = await _controller.ReserveSeats(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ReserveSeats_Should_Include_SubArea_In_Model()
    {
        // Arrange
        var eventWithSubArea = new Event
        {
            Id = 3,
            Name = "Theater Performance",
            VenueId = 1,
            EventTypeId = 1,
            SubAreaId = 2,
            StartDateTime = DateTime.Now.AddDays(2),
            EndTime = DateTime.Now.AddDays(2).AddHours(3)
        };
        _context.Event.Add(eventWithSubArea);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ReserveSeats(3);

        // Assert
        Assert.IsType<ViewResult>(result);
        var viewResult = result as ViewResult;
        var model = viewResult.Model as Event;

        Assert.NotNull(model);
        Assert.Equal(2, model.SubAreaId);
        Assert.NotNull(model.SubArea);
        Assert.Equal("Balcony", model.SubArea.AreaName);
    }

    [Fact]
    public async Task SelectSeats_With_Valid_Parameters_Should_Return_View()
    {
        // Arrange
        var subAreaId = 1;
        var eventId = 1;
        var duration = "2 hours";
        var resDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

        // Create event for testing
        var testEvent = new Event
        {
            Id = eventId,
            Name = "Test Event",
            VenueId = 1,
            EventTypeId = 1,
            SubAreaId = subAreaId,
            StartDateTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(1).AddHours(2)
        };
        _context.Event.Add(testEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.SelectSeats(eventId, subAreaId, duration, resDate);

        // Assert
        Assert.IsType<ViewResult>(result);
        var viewResult = result as ViewResult;

        Assert.Equal(eventId, viewResult.ViewData["EventId"]);
        Assert.Equal(subAreaId, viewResult.ViewData["SubAreaId"]);
        Assert.Equal(duration, viewResult.ViewData["Duration"]);
        Assert.Equal(resDate, viewResult.ViewData["ResDate"]);
        Assert.Equal("Test Venue", viewResult.ViewData["VenueName"]);
        Assert.Equal("Main Hall", viewResult.ViewData["SubAreaName"]);
    }

    [Fact]
    public async Task SelectSeats_With_Invalid_SubAreaId_Should_Return_NotFound()
    {
        // Act
        var result = await _controller.SelectSeats(1, 999, "2 hours", "2024-01-01");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SelectSeats_With_Invalid_EventId_Should_Return_NotFound()
    {
        // Act
        var result = await _controller.SelectSeats(999, 1, "2 hours", "2024-01-01");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(x => x.Value).Returns(new IdentityOptions());
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            options.Object,
            new Mock<IPasswordHasher<ApplicationUser>>().Object,
            new IUserValidator<ApplicationUser>[0],
            new IPasswordValidator<ApplicationUser>[0],
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<ApplicationUser>>>().Object);

        return userManagerMock;
    }

    private static Mock<RoleManager<ApplicationRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            store.Object,
            new IRoleValidator<ApplicationRole>[0],
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<Microsoft.Extensions.Logging.ILogger<RoleManager<ApplicationRole>>>().Object);

        return roleManagerMock;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}