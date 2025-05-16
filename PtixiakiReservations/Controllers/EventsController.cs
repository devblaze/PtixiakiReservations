using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;
using PtixiakiReservations.Services;
using System.Text;

namespace PtixiakiReservations.Controllers;

public class EventsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IElasticSearch elasticSearchService,
    ILogger<EventsController> logger)
    : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;

    // GET: Events
    public IActionResult Index()
    {
        return View();
    }

// New API endpoint to get today's events
    [HttpGet]
    public async Task<IActionResult> GetTodayEvents(string city, int page = 1, int pageSize = 12)
    {
        logger.LogInformation("Getting today's events. City filter: {City}", city ?? "None");

        var today = DateTime.Today;

        var eventsQuery = context.Event
            .Include(e => e.Venue)
            .ThenInclude(v => v.City)
            .Where(e => e.StartDateTime.Date == today)
            .OrderBy(e => e.StartDateTime);

        if (!string.IsNullOrWhiteSpace(city))
        {
            eventsQuery = (IOrderedQueryable<Event>)eventsQuery
                .Where(e => e.Venue.City.Name.ToLower() == city.ToLower());
        }

        var totalCount = await eventsQuery.CountAsync();
        var events = await eventsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        logger.LogInformation("Found {Count} today events", events.Count);

        return Json(new
        {
            events,
            totalCount,
            currentPage = page,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

// New API endpoint to get upcoming events
    [HttpGet]
    public async Task<IActionResult> GetUpcomingEvents(string city, int page = 1, int pageSize = 12)
    {
        logger.LogInformation("Getting upcoming events. City filter: {City}", city ?? "None");

        var today = DateTime.Today;

        var eventsQuery = context.Event
            .Include(e => e.Venue)
            .ThenInclude(v => v.City)
            .Where(e => e.StartDateTime.Date > today)
            .OrderBy(e => e.StartDateTime);

        if (!string.IsNullOrWhiteSpace(city))
        {
            eventsQuery = (IOrderedQueryable<Event>)eventsQuery
                .Where(e => e.Venue.City.Name.ToLower() == city.ToLower());
        }

        var totalCount = await eventsQuery.CountAsync();
        var events = await eventsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        logger.LogInformation("Found {Count} upcoming events", events.Count);

        return Json(new
        {
            events,
            totalCount,
            currentPage = page,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

// New API endpoint to get past events
    [HttpGet]
    public async Task<IActionResult> GetPastEvents(string city, int page = 1, int pageSize = 12)
    {
        logger.LogInformation("Getting past events. City filter: {City}", city ?? "None");

        var today = DateTime.Today;

        var eventsQuery = context.Event
            .Include(e => e.Venue)
            .ThenInclude(v => v.City)
            .Where(e => e.StartDateTime.Date < today)
            .OrderByDescending(e => e.StartDateTime); // Show most recent past events first

        if (!string.IsNullOrWhiteSpace(city))
        {
            eventsQuery = (IOrderedQueryable<Event>)eventsQuery
                .Where(e => e.Venue.City.Name.ToLower() == city.ToLower());
        }

        var totalCount = await eventsQuery.CountAsync();
        var events = await eventsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        logger.LogInformation("Found {Count} past events", events.Count);

        return Json(new
        {
            events,
            totalCount,
            currentPage = page,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

// New API endpoint to get all events
    [HttpGet]
    public async Task<IActionResult> GetAllEvents(string city, int page = 1, int pageSize = 12)
    {
        logger.LogInformation("Getting all events. City filter: {City}", city ?? "None");

        var eventsQuery = context.Event
            .Include(e => e.Venue)
            .ThenInclude(v => v.City)
            .OrderBy(e => e.StartDateTime);

        if (!string.IsNullOrWhiteSpace(city))
        {
            eventsQuery = (IOrderedQueryable<Event>)eventsQuery
                .Where(e => e.Venue.City.Name.ToLower() == city.ToLower());
        }

        var totalCount = await eventsQuery.CountAsync();
        var events = await eventsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        logger.LogInformation("Found {Count} total events", events.Count);

        return Json(new
        {
            events,
            totalCount,
            currentPage = page,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public string GetEventTimeClass(DateTime eventDate)
    {
        DateTime today = DateTime.Today;

        if (eventDate.Date == today)
        {
            return "event-today";
        }
        else if (eventDate.Date > today)
        {
            return "event-upcoming";
        }
        else
        {
            return "event-past";
        }
    }

    public async Task<IActionResult> EventsForToday(string city, int page = 1, int pageSize = 12)
    {
        logger.LogInformation("Fetching events for today and future. City filter: {City}", city ?? "None");

        var today = DateTime.Today;

        var eventsQuery = context.Event
            .Include(e => e.Venue)
            .ThenInclude(v => v.City)
            .Where(e => e.StartDateTime.Date >= today.AddDays(-30) &&
                        e.StartDateTime.Date <= today.AddDays(60))
            .OrderBy(e => e.StartDateTime);

        if (!string.IsNullOrWhiteSpace(city))
        {
            eventsQuery = (IOrderedQueryable<Event>)eventsQuery
                .Where(e => e.Venue.City.Name.ToLower() == city.ToLower());
        }

        var eventsList = await eventsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Add debug info to see event dates
        var todayEvents = eventsList.Count(e => e.StartDateTime.Date == today);
        var upcomingEvents = eventsList.Count(e => e.StartDateTime.Date > today);
        var pastEvents = eventsList.Count(e => e.StartDateTime.Date < today);

        logger.LogInformation(
            "Found {Count} events. Distribution: Today: {TodayCount}, Upcoming: {UpcomingCount}, Past: {PastCount}",
            eventsList.Count,
            todayEvents,
            upcomingEvents,
            pastEvents);

        return View(eventsList);
    }

    // GET: Events/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var @event = await context.Event
            .Include(r => r.FamilyEvent)
            .Include(r => r.Venue)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (@event is null) return NotFound();

        return View(@event);
    }

    // GET: Events/Create      
    public JsonResult GetEvents()
    {
        var events = context.Event.Include(e => e.EventType)
            .Where(e => e.Venue.ApplicationUser.Id == userManager.GetUserId(HttpContext.User)).ToList();
        return new JsonResult(events);
    }

    public JsonResult GetEvents2(int? venueId)
    {
        var events = context.Event.Where(e => e.Venue.Id == venueId).ToList();

        return new JsonResult(events);
    }

    public JsonResult GetEventTypes()
    {
        var eventsTypes = context.EventType.ToList();

        return new JsonResult(eventsTypes);
    }

    public async Task<IActionResult> VenueEvents(int venueId)
    {
        var venue = await context.Venue.FirstOrDefaultAsync(v => v.Id == venueId);

        if (venue is null) return NotFound();

        return View(venue);
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        // Get the current user ID
        var userId = userManager.GetUserId(User);

        // Fetch the venues associated with the current user
        var venues = await context.Venue
            .Where(v => v.UserId == userId)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = v.Name
            }).ToListAsync();

        // Pass the venues to the view via ViewBag
        ViewBag.VenueList = venues;

        var eventTypes = context.EventType.ToList();
        ViewBag.EventTypeList = new SelectList(eventTypes, "Id", "Name");

        return View(new Event());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(Event newEvent)
    {
        if (!ModelState.IsValid)
        {
            // Reload venues for the dropdown
            var userId = userManager.GetUserId(User);
            ViewBag.VenueList = await context.Venue
                .Where(v => v.UserId == userId)
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.Name
                }).ToListAsync();

            return View(newEvent); // Return the form with validation errors
        }

        newEvent.FamilyEventId ??= 1;

        context.Event.Add(newEvent);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(EventsForToday));
    }

    public bool CorrectDay(JsonEventModel ev, int i, int everyNum)
    {
        bool correctDay = false;
        if (ev.Repeat.M == true && ev.StartDateTime.AddDays(i + everyNum * 7).DayOfWeek.ToString() == "Monday")
        {
            correctDay = true;
        }
        else if (ev.Repeat.Tu == true && ev.StartDateTime.AddDays(i + everyNum * 7).DayOfWeek.ToString() == "Tuesday")
        {
            correctDay = true;
        }
        else if (ev.Repeat.W == true && ev.StartDateTime.AddDays(i + everyNum * 7).DayOfWeek.ToString() == "Wednesday")
        {
            correctDay = true;
        }
        else if (ev.Repeat.Th == true && ev.StartDateTime.AddDays(i + everyNum * 7).DayOfWeek.ToString() == "Thursday")
        {
            correctDay = true;
        }
        else if (ev.Repeat.F == true && ev.StartDateTime.AddDays(i + everyNum * 7).DayOfWeek.ToString() == "Friday")
        {
            correctDay = true;
        }
        else if (ev.Repeat.Sa == true && ev.StartDateTime.AddDays(i + everyNum * 7).DayOfWeek.ToString() == "Saturday")
        {
            correctDay = true;
        }
        else if (ev.Repeat.Su == true && ev.StartDateTime.AddDays(i + everyNum * 7).DayOfWeek.ToString() == "Sunday")
        {
            correctDay = true;
        }
        return correctDay;
    }

    public async Task<IActionResult> Delete(int? id, bool dAll)
    {
        if (id == null)
        {
            return NotFound();
        }
        var ev = await context.Event
            .Include(r => r.FamilyEvent)
            .Include(r => r.Venue)
            .Include(r => r.EventType)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (ev == null)
        {
            return NotFound();
        }
        if (dAll == true)
        {
            var familyEvents = context.Event.Include(r => r.FamilyEvent).Include(r => r.EventType)
                .Where(e => e.FamilyEventId == ev.FamilyEventId).ToList();
            foreach (var @event in familyEvents)
            {
                var hasReservations = context.Reservation.Where(r => r.EventId == @event.Id).ToList();
                context.Reservation.RemoveRange(hasReservations);
            }
            context.Event.RemoveRange(familyEvents);
        }
        else
        {
            var hasReservations = context.Reservation.Where(r => r.EventId == ev.Id).ToList();
            context.Reservation.RemoveRange(hasReservations);
            context.Event.Remove(ev);
        }

        await context.SaveChangesAsync();
        Response.StatusCode = (int)HttpStatusCode.OK;
        return Json(Response.StatusCode);
    }

    private bool EventExists(int id)
    {
        return context.Event.Any(e => e.Id == id);
    }

    // Index events into Elasticsearch
    [HttpPost]
    public async Task<IActionResult> IndexEventsToElastic()
    {
        var events = new List<Event>
        {
            new Event
            {
                Id = 1, Name = "Concert", StartDateTime = DateTime.Now, EndTime = DateTime.Now.AddHours(2)
            },
            new Event
            {
                Id = 2, Name = "Conference", StartDateTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(1).AddHours(3)
            }
        };

        await elasticSearchService.CreateIndexIfNotExistsAsync("events");
        var result = await elasticSearchService.AddOrUpdateBulkAsync(events, "events");

        return result ? Ok("Events indexed successfully!") : BadRequest("Failed to index events.");
    }

    /// <summary>
    /// Advanced search for events with date range filtering and elasticsearch support
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchEvents(
        string eventTypeId = null,
        string startDate = null,
        string endDate = null,
        string searchTerm = null,
        int page = 1,
        int pageSize = 12)
    {
        logger.LogInformation(
            "Event search with criteria: EventType={EventType}, StartDate={StartDate}, EndDate={EndDate}, SearchTerm={SearchTerm}",
            eventTypeId,
            startDate,
            endDate,
            searchTerm);

        try
        {
            // Count filled search criteria
            int filledCriteria = 0;
            if (!string.IsNullOrWhiteSpace(eventTypeId)) filledCriteria++;
            if (!string.IsNullOrWhiteSpace(startDate)) filledCriteria++;
            if (!string.IsNullOrWhiteSpace(endDate)) filledCriteria++;
            if (!string.IsNullOrWhiteSpace(searchTerm)) filledCriteria++;

            // If using date filtering, ensure we have valid dates
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;

            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out DateTime startDateValue))
            {
                parsedStartDate = startDateValue.Date;
            }

            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out DateTime endDateValue))
            {
                // Set to end of day for inclusive filtering
                parsedEndDate = endDateValue.Date.AddDays(1).AddSeconds(-1);
            }

            // Use standard database query for better date filtering
            var query = context.Event
                .Include(e => e.Venue)
                .ThenInclude(v => v.City)
                .AsQueryable();

            // Apply filters based on provided criteria
            if (!string.IsNullOrWhiteSpace(eventTypeId) && int.TryParse(eventTypeId, out int eventTypeIdValue))
            {
                query = query.Where(e => e.EventTypeId == eventTypeIdValue);
            }

            // Apply date range filtering
            if (parsedStartDate.HasValue)
            {
                query = query.Where(e => e.StartDateTime >= parsedStartDate.Value);
            }

            if (parsedEndDate.HasValue)
            {
                query = query.Where(e => e.StartDateTime <= parsedEndDate.Value);
            }

            // Apply text search if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.ToLower();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(term) ||
                    e.Venue.Name.ToLower().Contains(term) ||
                    e.Venue.City.Name.ToLower().Contains(term)
                );
            }

            // Order by start date (nearest first)
            query = query.OrderBy(e => e.StartDateTime);

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var events = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Return results as JSON
            return Json(new
            {
                events,
                totalCount,
                currentPage = page,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing event search");
            return StatusCode(500, "An error occurred while searching for events");
        }
    }

    [HttpPost]
    public async Task<IActionResult> IndexAllEventsToElastic()
    {
        try
        {
            // Load all events with necessary includes
            var events = await context.Event
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .ToListAsync();

            // Create the index if it doesn't exist
            await elasticSearchService.CreateIndexIfNotExistsAsync("events");

            // Index events in batches
            const int batchSize = 50;
            var successCount = 0;

            for (int i = 0; i < events.Count; i += batchSize)
            {
                var batch = events.Skip(i).Take(batchSize).ToList();
                var result = await elasticSearchService.AddOrUpdateBulkAsync(batch, "events");

                if (result)
                {
                    successCount += batch.Count;
                }
            }

            return Ok($"Successfully indexed {successCount} of {events.Count} events to Elasticsearch.");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error indexing events: {ex.Message}");
        }
    }

    [HttpGet("test-elasticsearch")]
    [AllowAnonymous] // Allow access without authentication for testing
    public async Task<IActionResult> TestElasticsearch()
    {
        try
        {
            // Test 1: Check if we can create an index
            var indexName = "test-index";
            var createResult = await elasticSearchService.CreateIndexIfNotExistsAsync(indexName);

            if (!createResult)
            {
                return BadRequest("Failed to create Elasticsearch index");
            }

            // Test 2: Index a simple document
            var testEvent = new Event
            {
                Id = 999,
                Name = "Test Event " + DateTime.Now.Ticks,
                StartDateTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                EventTypeId = 1,
                VenueId = 1
            };

            var indexResult = await elasticSearchService.AddOrUpdateAsync(testEvent, indexName);

            if (!indexResult)
            {
                return BadRequest("Failed to index test document");
            }

            // Test 3: Search for the document
            var searchResults = await elasticSearchService.SearchAsync<Event>("Test Event", indexName);

            return Ok(new
            {
                message = "Elasticsearch is working!",
                indexCreated = createResult,
                documentIndexed = indexResult,
                searchResults = searchResults.Select(e => new { e.Id, e.Name, e.StartDateTime })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing Elasticsearch");
            return BadRequest($"Elasticsearch test failed: {ex.Message}");
        }
    }

    // GET: Events/GenerateEvents/{count}
    [HttpGet]
    [Route("Events/GenerateEvents/{count}")]
    public async Task<IActionResult> GenerateEvents(int count)
    {
        if (count <= 0 || count > 100)
        {
            return BadRequest("The count must be between 1 and 100.");
        }

        var now = DateTime.Now;
        var eventTypes = await context.EventType.ToListAsync();
        var venues = await context.Venue.ToListAsync();

        if (!eventTypes.Any() || !venues.Any())
        {
            return BadRequest("No event types or venues available for event generation.");
        }

        var random = new Random();
        var generatedEvents = new List<Event>();

        for (int i = 0; i < count; i++)
        {
            // Pick random event type and venue
            var eventType = eventTypes[random.Next(eventTypes.Count)];
            var venue = venues[random.Next(venues.Count)];

            // Random date between now and 3 months in the future
            var daysToAdd = random.Next(1, 5);
            var startDate = now.AddDays(daysToAdd);

            // Event duration between 1 and 4 hours
            var duration = random.Next(1, 5);

            var newEvent = new Event
            {
                Name = $"Generated Event {i + 1}",
                StartDateTime = startDate,
                EndTime = startDate.AddHours(duration),
                EventTypeId = eventType.Id,
                VenueId = venue.Id
            };

            context.Event.Add(newEvent);
            generatedEvents.Add(newEvent);
        }

        await context.SaveChangesAsync();

        logger.LogInformation("Generated {Count} new events", count);

        return Json(new
        {
            success = true,
            message = $"Successfully generated {count} events",
            events = generatedEvents
        });
    }
}