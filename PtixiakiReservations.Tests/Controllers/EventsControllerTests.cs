using Moq;
using Microsoft.AspNetCore.Mvc;
using PtixiakiReservations.Controllers;
using PtixiakiReservations.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Services;

namespace PtixiakiReservations.Tests.Controllers;

public class EventsControllerTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<IElasticSearch> _elasticSearchMock;
    private readonly ApplicationDbContext _context;

    public EventsControllerTests()
    {
        // Mock dependencies
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();
        _elasticSearchMock = new Mock<IElasticSearch>();

        // Create a unique database instance for each test
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

        _context = new ApplicationDbContext(options);
        SeedDatabase(_context);
    }

    [Fact]
    public async Task Details_ReturnsViewResult_WhenEventExists()
    {
        // Arrange
        var controller = new EventsController(_context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object);
        int testEventId = 1; // Seeded ID

        // Act
        var result = await controller.Details(testEventId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Event>(viewResult.Model);
        Assert.Equal(testEventId, model.Id);
    }

    [Fact]
    public async Task Details_ReturnsNotFoundResult_WhenEventDoesNotExist()
    {
        // Arrange
        var controller = new EventsController(_context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _elasticSearchMock.Object);

        // Act
        var result = await controller.Details(-1); // Non-existent ID

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<RoleManager<ApplicationRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        return new Mock<RoleManager<ApplicationRole>>(store.Object, null, null, null, null);
    }

    private static void SeedDatabase(ApplicationDbContext context)
    {
        // Clear existing data
        context.Event.RemoveRange(context.Event);
        context.SaveChanges();

        // Seed sample data for testing
        context.Event.Add(new Event
        {
            Id = 1,
            Name = "Test Event",
            StartDateTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(2),
            VenueId = 1
        });
        context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}