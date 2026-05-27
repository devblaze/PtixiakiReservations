using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
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
using System.Text.Json; 
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Http;    

namespace PtixiakiReservations.Controllers;

public class EventsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IElasticSearch elasticSearchService,
    ILogger<EventsController> logger,
    IWebHostEnvironment environment)
    : Controller
{
    // GET: Events
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
    {
        var query = context.Event.AsQueryable();

        int totalCount = await query.CountAsync();


        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize);
        return View();
    }

    // New API endpoint to get today's events
    [HttpGet]
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    [AllowAnonymous]
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

   [AllowAnonymous]
    public async Task<IActionResult> EventsForToday(int? category, string city, string searchTerm, int page = 1, int pageSize = 12)
    {
        var today = DateTime.Today;
        var eventsQuery = context.Event
            .Include(e => e.Venue)
            .ThenInclude(v => v.City)
            .Include(e => e.EventType) 
            .Where(e => e.ParentEventId == null); 

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            eventsQuery = eventsQuery.Where(e => 
                e.Name.ToLower().Contains(term) || 
                (e.Venue != null && e.Venue.Name.ToLower().Contains(term)) || 
                (e.Venue != null && e.Venue.City != null && e.Venue.City.Name.ToLower().Contains(term))
            );
            ViewBag.SearchTerm = searchTerm; 
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            eventsQuery = eventsQuery.Where(e => e.Venue.City.Name.ToLower() == city.ToLower());
        }

        if (category.HasValue && category.Value > 0)
        {
            eventsQuery = eventsQuery.Where(e => e.EventTypeId == category.Value);
        }

        eventsQuery = eventsQuery.OrderBy(e => e.StartDateTime);

        int totalMasterEvents = await eventsQuery.CountAsync();


        ViewBag.TotalMasterEvents = totalMasterEvents; 
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalMasterEvents / pageSize);
        ViewBag.CurrentPage = page;

        var eventsList = await eventsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var masterIds = eventsList.Select(e => e.Id).ToList();

        var childCounts = await context.Event
            .Where(e => e.ParentEventId != null && masterIds.Contains(e.ParentEventId.Value))
            .GroupBy(e => e.ParentEventId.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count);

        ViewBag.ChildCounts = childCounts;

        return View(eventsList);
    }

// GET: Events/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventDetails = await context.Event
            .Include(e => e.Venue)
            .Include(e => e.ParentEvent) 
            .Include(e => e.Venue.City)
            .Include(e => e.EventType)
            .Include(e => e.ChildEvents) 
            .ThenInclude(c => c.SubArea)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (eventDetails == null)
        {
            return NotFound();
        }

        return View(eventDetails);
    }

    // Methods that require authentication

    // GET: Events/Create  
    [Authorize]
    public JsonResult GetEvents()
    {
        var events = context.Event.Include(e => e.EventType)
            .Where(e => e.Venue.ApplicationUser.Id == userManager.GetUserId(HttpContext.User)).ToList();
        return new JsonResult(events);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetEvents2(int? venueId)
    {

        var events = context.Event
            .Where(e => e.VenueId == venueId)
            .Include(e => e.EventType) 
            .ToList();


        var result = events.Select(e => new {
            id = e.Id,
            name = e.Name,
            startDateTime = e.StartDateTime,
            endTime = e.EndTime,

            eventType = e.EventType != null ? e.EventType.Name : "Default" 
        });

        return Json(result); 
    }

    [AllowAnonymous]
    public JsonResult GetEventTypes()
    {
        var eventsTypes = context.EventType.ToList();

        return new JsonResult(eventsTypes);
    }

    [Authorize]
    public async Task<IActionResult> VenueEvents(int venueId)
    {
        var venue = await context.Venue.FirstOrDefaultAsync(v => v.Id == venueId);

        if (venue is null) return NotFound();

        return View(venue);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        try
        {
            // Get the current user ID
            var userId = userManager.GetUserId(User);
            logger.LogInformation("Creating event form for user: {UserId}", userId);

            // Fetch the venues associated with the current user
            var venues = await context.Venue
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.Name
                }).ToListAsync();

            // Check if there are any venues
            if (venues.Count == 0)
            {
                logger.LogWarning("User {UserId} has no venues to create events for", userId);
                TempData["ErrorMessage"] = "You need to create a venue before you can create an event.";
                return RedirectToAction("Create", "Venue");
            }

            // Pass the venues to the view via ViewBag
            ViewBag.VenueList = venues;

            // Get event types for dropdown
            var eventTypes = await context.EventType.ToListAsync();
            if (eventTypes.Count == 0)
            {
                logger.LogWarning("No event types found in the database");
                TempData["ErrorMessage"] = "No event types are available. Please contact an administrator.";
                return RedirectToAction("Index");
            }

            ViewBag.EventTypeList = new SelectList(eventTypes, "Id", "Name");

            // Return the create form with an empty Event model
            return View(new Event());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error preparing create event form");
            TempData["ErrorMessage"] = "An error occurred while preparing the form. Please try again.";
            return RedirectToAction("Index");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(
        Event newEvent,
        IFormFile? imageFile,
        string IsMultiDay = null,
        string StartTime = null,
        string MultiEndTime = null,
        string SpecificDatesJson = null)
    {
        bool isMultiDay = IsMultiDay == "on" || IsMultiDay == "true";
        var userId = userManager.GetUserId(User);

        // 1. Handle Image Upload First
        if (imageFile != null && imageFile.Length > 0)
        {
            try
            {
                string uploadsFolder = Path.Combine(environment.WebRootPath, "images/events");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                newEvent.ImagePath = "/images/events/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving event image to disk.");
                // Returning BadRequest so the fetch API knows it failed
                return BadRequest(new { success = false, message = "Error saving image." });
            }
        }

        try
        {
            logger.LogInformation("Processing event creation. isMultiDay: {isMultiDay}", isMultiDay);

            // 2. Validate Model State
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                logger.LogWarning("Model state is invalid.");
                return BadRequest(new { success = false, message = "Invalid form data.", errors });
            }

            // 3. Verify venue belongs to the current user
            var venue = await context.Venue.FirstOrDefaultAsync(v => v.Id == newEvent.VenueId);
            if (venue == null)
            {
                logger.LogWarning("Venue {VenueId} not found", newEvent.VenueId);
                return BadRequest(new { success = false, message = "Venue does not exist." });
            }

            // Variable to hold our newly created Father event, declared out here 
            // so we can grab its ID at the very end of the function!
            Event fatherEvent = null;

            // 4. Handle Save Logic
            if (isMultiDay && !string.IsNullOrEmpty(SpecificDatesJson)
                && !string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(MultiEndTime))
            {
                logger.LogInformation("Creating multi-day events");

                var selectedDates = JsonSerializer.Deserialize<List<string>>(SpecificDatesJson);
                
                TimeSpan startTimeSpan = DateTime.TryParse(StartTime, out DateTime pst) ? pst.TimeOfDay : TimeSpan.Parse(StartTime);
                TimeSpan endTimeSpan = DateTime.TryParse(MultiEndTime, out DateTime pet) ? pet.TimeOfDay : TimeSpan.Parse(MultiEndTime);

                var count = 1;
                foreach (var dateString in selectedDates)
                {
                    if (DateTime.TryParse(dateString, out DateTime date))
                    {

                        var eventForDay = new Event
                        {
                            Name = newEvent.Name+" Day "+count,
                            VenueId = newEvent.VenueId,
                            EventTypeId = newEvent.EventTypeId,
                            SubAreaId = newEvent.SubAreaId,
                            StartDateTime = date.Date.Add(startTimeSpan),
                            EndTime = date.Date.Add(endTimeSpan),
                            ImagePath = newEvent.ImagePath 
                        };

                        if (newEvent.ParentEventId.HasValue)
                        {
                            eventForDay.ParentEventId = newEvent.ParentEventId;
                            context.Add(eventForDay);
                        }
                        count++;
                    }
                }
            }
            else
            {
                // Single event creation
                logger.LogInformation("Creating single event on {Date}", newEvent.StartDateTime);

                if (newEvent.StartDateTime == DateTime.MinValue)
                    newEvent.StartDateTime = DateTime.Now;

                if (newEvent.EndTime == DateTime.MinValue)
                    newEvent.EndTime = newEvent.StartDateTime.AddHours(2);

                context.Add(newEvent);
            }

            // Save everything to the database
            await context.SaveChangesAsync();

            // 5. DETERMINE WHAT ID TO SEND BACK TO JAVASCRIPT
            int? returnedEventId = newEvent.ParentEventId; 

            // If ParentEventId is null, they just created a brand new Master Event!
            if (returnedEventId == null)
            {
                // If they did a multi-day master event, the ID we want is the fatherEvent
                if (isMultiDay && fatherEvent != null)
                {
                    returnedEventId = fatherEvent.Id;
                }
                else // If they did a single day master event, the ID is just the newEvent
                {
                    returnedEventId = newEvent.Id;
                }
            }

            // Return the JSON data so the JavaScript can automatically switch to Sub-Event Mode
            return Json(new { 
            success = true, 
            eventId = returnedEventId, 
            eventName = newEvent.Name,
            venueId = newEvent.VenueId,
            venueName = venue.Name,
            eventTypeId = newEvent.EventTypeId
        });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating event");
            return BadRequest(new { success = false, message = "An error occurred while creating the event." });
        }
    }

// Private helper to keep the Create function clean
private async Task ReloadCreateDropdowns(string userId)
{
    ViewBag.VenueList = await context.Venue
        .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Name })
        .ToListAsync();

    ViewBag.EventTypeList = new SelectList(await context.EventType.ToListAsync(), "Id", "Name");
}

    [Authorize]
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

    [Authorize]
    public async Task<IActionResult> Delete(int? id, bool dAll)
    {
        if (id == null)
        {
            return NotFound();
        }

        var ev = await context.Event
            .Include(r => r.ParentEvent) 
            .Include(r => r.Venue)
            .Include(r => r.EventType)
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (ev == null)
        {
            return NotFound();
        }

        if (dAll == true)
        {
            // Figure out the parent ID (if ev IS the parent, use its own Id)
            var targetParentId = ev.ParentEventId ?? ev.Id;

            // Grab the parent and all its children
            var relatedEvents = context.Event
                .Include(r => r.ParentEvent)
                .Include(r => r.EventType)
                .Where(e => e.Id == targetParentId || e.ParentEventId == targetParentId)
                .ToList();

            foreach (var @event in relatedEvents)
            {
                var hasReservations = context.Reservation.Where(r => r.EventId == @event.Id).ToList();
                context.Reservation.RemoveRange(hasReservations);
            }

            context.Event.RemoveRange(relatedEvents);
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
    [Authorize]
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
        return Ok(result);
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
        string sort = "asc",
        int page = 1,
        int pageSize = 12)
    {
        logger.LogInformation(
            "Event search with criteria: EventType={EventType}, StartDate={StartDate}, EndDate={EndDate}, SearchTerm={SearchTerm}, Sort={Sort}",
            eventTypeId,
            startDate,
            endDate,
            searchTerm,
            sort);

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

            // Determine if the user is filtering by date
            bool hasDateFilter = !string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate);

            // Use standard database query
            var query = context.Event
                .Include(e => e.Venue)
                .ThenInclude(v => v.City)
                .AsQueryable();

            // ONLY apply the Parent-only filter if NO date filter was provided
            if (!hasDateFilter)
            {
                query = query.Where(e => e.ParentEventId == null);
            }

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
            if (sort == "desc")
            {
                query = query.OrderByDescending(e => e.StartDateTime);
            }
            else
            {
                query = query.OrderBy(e => e.StartDateTime);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var events = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.StartDateTime,
                    e.EndTime,
                    ImagePath = e.ImagePath ?? e.ParentEvent.ImagePath,
                    VenueName = e.Venue.Name,
                    CityName = e.Venue.City != null ? e.Venue.City.Name : "N/A",
                    parentEventId = e.ParentEventId,
                    childCount = context.Event.Count(c => c.ParentEventId == e.Id)
                })
                .ToListAsync();

            // Return results as JSON
            return Json(new
            {
                events, // This now contains the projected objects with the ImagePath
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
        if (count <= 0 || count > 500)
        {
            return BadRequest("The count must be between 1 and 500.");
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

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUserEvents()
    {
        try
        {
            var query = context.Event.AsQueryable();

            var events = await query
                .OrderByDescending(e => e.StartDateTime)
                .Select(e => new {
                    id = e.Id,
                    name = e.Name,
                    startDateTime = e.StartDateTime,
                    endTime = e.EndTime,
                    venueId = e.VenueId,
                    venue = e.Venue != null ? new { name = e.Venue.Name } : null,
                    eventType = e.EventType != null ? new { name = e.EventType.Name } : null
                })
                .ToListAsync();

            return Json(new { success = true, events = events });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [AllowAnonymous]
    public async Task<IActionResult> GetAutocompleteResults(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
        {
            return Json(new List<object>());
        }

        query = query.ToLower();
        var results = new List<object>();
        var maxResults = 5;

        try
        {
            // Search for events with matching names
            var eventResults = await context.Event
                .Where(e => e.Name.ToLower().Contains(query))
                .OrderBy(e => e.Name)
                .Take(maxResults)
                .Select(e => new
                {
                    text = e.Name,
                    type = "event",
                    subtext = $"Event on {e.StartDateTime.ToString("MMM d, yyyy")}",
                    id = e.Id
                })
                .ToListAsync();

            results.AddRange(eventResults);

            // If we need more results, search for venues
            if (results.Count < maxResults)
            {
                var remainingSlots = maxResults - results.Count;
                var venueResults = await context.Venue
                    .Where(v => v.Name.ToLower().Contains(query))
                    .OrderBy(v => v.Name)
                    .Take(remainingSlots)
                    .Select(v => new
                    {
                        text = v.Name,
                        type = "location",
                        subtext = v.City != null ? $"Venue in {v.City.Name}" : "Venue",
                        id = v.Id
                    })
                    .ToListAsync();

                results.AddRange(venueResults);
            }

            // If we still need more results, search for cities
            if (results.Count < maxResults)
            {
                var remainingSlots = maxResults - results.Count;
                var cityResults = await context.City
                    .Where(c => c.Name.ToLower().Contains(query))
                    .OrderBy(c => c.Name)
                    .Take(remainingSlots)
                    .Select(c => new
                    {
                        text = c.Name,
                        type = "location",
                        subtext = "City",
                        id = c.Id
                    })
                    .ToListAsync();

                results.AddRange(cityResults);
            }

            return Json(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching autocomplete results for query: {Query}", query);
            return Json(new List<object>());
        }
    }

    [HttpGet]
    public JsonResult GetSubAreas(int venueId)
    {
        var subAreas = context.SubArea
            .Where(sa => sa.VenueId == venueId)
            .Select(sa => new { id = sa.Id, areaName = sa.AreaName })
            .ToList();

        return Json(subAreas);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Get the current user ID
        var userId = userManager.GetUserId(User);

        // Find the event and include related entities
        var eventToEdit = await context.Event
            .Include(e => e.Venue)
            .Include(e => e.EventType)
            .Include(e => e.SubArea)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventToEdit == null)
        {
            return NotFound();
        }

       

        // Get venues for dropdown
        var venues = await context.Venue
            .Where(v => v.UserId == userId)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = v.Name
            }).ToListAsync();

        ViewBag.VenueList = venues;
        ViewBag.EventTypeList =
            new SelectList(await context.EventType.ToListAsync(), "Id", "Name", eventToEdit.EventTypeId);

        // Get layouts for the selected venue
        var subAreas = await context.SubArea
            .Where(sa => sa.VenueId == eventToEdit.VenueId)
            .Select(sa => new SelectListItem
            {
                Value = sa.Id.ToString(),
                Text = sa.AreaName
            }).ToListAsync();

        ViewBag.SubAreaList = subAreas;

        return View(eventToEdit);
    }

   
    

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Event updatedEvent,IFormFile? imageFile)
    {
        if (id != updatedEvent.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Get the original event to update only allowed fields
                var originalEvent = await context.Event.FindAsync(id);
                if (originalEvent == null)
                {
                    return NotFound();
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(originalEvent.ImagePath))
                    {

                        string oldFilePath = Path.Combine(environment.WebRootPath, originalEvent.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    try
                    {
                        string uploadsFolder = Path.Combine(environment.WebRootPath, "images/events");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        originalEvent.ImagePath = "/images/events/" + uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error saving event image to disk.");
                        ModelState.AddModelError("", "Error saving image.");
                        return View(updatedEvent);
                    }
                }

                originalEvent.Name = updatedEvent.Name;
                originalEvent.StartDateTime = updatedEvent.StartDateTime;
                originalEvent.EndTime = updatedEvent.EndTime;
                originalEvent.EventTypeId = updatedEvent.EventTypeId;
                originalEvent.VenueId = updatedEvent.VenueId;
                originalEvent.SubAreaId = updatedEvent.SubAreaId;
                originalEvent.ParentEventId = originalEvent.ParentEventId;


                await context.SaveChangesAsync();

                var userId = userManager.GetUserId(User);
                logger.LogInformation("Event {EventId} updated successfully by Admin {UserId}", id, userId);
                
                TempData["SuccessMessage"] = "Event updated successfully.";

                return RedirectToAction(nameof(VenueEvents), new { venueId = updatedEvent.VenueId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Concurrency error when updating event {EventId}", id);

                if (!EventExists(updatedEvent.Id))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError("", "The event was modified by another user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating event {EventId}", id);
                ModelState.AddModelError("", "An error occurred while updating the event. Please try again.");
            }
        }

        ViewBag.VenueList = await context.Venue
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = v.Name
            }).ToListAsync();

        ViewBag.EventTypeList = new SelectList(await context.EventType.ToListAsync(), "Id", "Name");

        // Get layouts for the selected venue
        ViewBag.SubAreaList = await context.SubArea
            .Where(sa => sa.VenueId == updatedEvent.VenueId)
            .Select(sa => new SelectListItem
            {
                Value = sa.Id.ToString(),
                Text = sa.AreaName
            }).ToListAsync();

        return View(updatedEvent);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> EditSubSelectedName(int id, string NewName)
    {
        try
            {
                var originalEvent = await context.Event.FindAsync(id);
                if (originalEvent == null)
                {
                    return NotFound();
                }
                originalEvent.Name = NewName;

                await context.SaveChangesAsync();
            
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating event {EventId}", id);
                return NotFound();
            }
            return Json(new { 
                success = true, 
                message = "Your event was renamed successfully!" 
            });
    }

    [Authorize] 
    [HttpGet]
    public async Task<IActionResult> SearchParentEvents(string query)
    {
        var events = await context.Event
            .Where(e => e.ParentEventId == null && e.Name.Contains(query))
            .Select(e => new {
                id = e.Id,
                name = e.Name,
                venue = e.Venue.Name,
                venueId = e.VenueId, // Necessary for inheritance
                eventTypeId = e.EventTypeId,
                rawStartDate = e.StartDateTime.ToString("yyyy-MM-ddTHH:mm"),
                rawEndDate = e.EndTime.ToString("yyyy-MM-ddTHH:mm"),
                date = e.StartDateTime.ToString("MMM dd, yyyy")
            })
            .Take(5)
            .ToListAsync();
        return Json(events);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetSubEvents(int parentId)
    {
        var subEvents = await context.Event
            .Include(e => e.SubArea)
            .Where(e => e.ParentEventId == parentId)
            .OrderBy(e => e.StartDateTime)
            .Select(e => new {
                id = e.Id,
                name = e.Name, 
                date = e.StartDateTime.ToString("dddd, MMM d, yyyy"),
                time = e.StartDateTime.ToString("h:mm tt") + " - " + e.EndTime.ToString("h:mm tt"),
                layout= e.SubArea.AreaName
            })
            .ToListAsync();
            
        return Json(subEvents);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateParentEvent([FromBody] LinkEventDto data)
    {
        try
        {
            var userId = userManager.GetUserId(User);
            
            // Find the child event we are editing
            var childEvent = await context.Event
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == data.ChildId);

            if (childEvent == null) return NotFound(new { success = false, message = "Event not found." });

            // Security check: ensure they own the venue for this event
            if (childEvent.Venue.UserId != userId) 
                return Unauthorized(new { success = false, message = "Not authorized." });

            // Update the relationship
            childEvent.ParentEventId = data.ParentId;
            await context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error async saving parent event");
            return StatusCode(500, new { success = false, message = "Server error." });
        }
    }

    // A tiny helper class to catch the JSON data
    public class LinkEventDto
    {
        public int ChildId { get; set; }
        public int? ParentId { get; set; } // Nullable in case they clear it
    }

    [AllowAnonymous]
    public async Task<IActionResult> ChildEvents(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // 1. Fetch the Father event just to get its name for the page title
        var fatherEvent = await context.Event.FirstOrDefaultAsync(e => e.Id == id);
        if (fatherEvent == null)
        {
            return NotFound();
        }

        // 2. Fetch all Child events that belong to this Father
        var childEvents = await context.Event
            .Include(e => e.Venue)
            .Include(e => e.Venue.City)
            .Include(e => e.EventType)
            .Where(e => e.ParentEventId == id)
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();

        // Pass the Father's info to the view using ViewBag
        ViewBag.FatherEventName = fatherEvent.Name;
        ViewBag.FatherEventId = fatherEvent.Id;

        return View(childEvents);
    }

    [HttpGet]
    public async Task<IActionResult> SearchVenues(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new List<object>());

        query = query.ToLower();

        var venues = await context.Venue
            .Where(v =>
                v.Name.ToLower().Contains(query) ||
                (v.City != null && v.City.Name.ToLower().Contains(query))
            )
            .OrderByDescending(v => v.Name.ToLower().Contains(query))
            .ThenBy(v => v.Name)
            .Select(v => new
            {
                id = v.Id,
                name = v.Name,
                city = v.City != null ? v.City.Name : "N/A"
            })
            .Take(10)
            .ToListAsync();

        return Json(venues);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DuplicateSubEvent(int id)
    {
        var ev = await context.Event
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
        {
            return NotFound();
        }


        var newEvent = new Event
        {
            Name = GenerateNextName(ev.Name),
            StartDateTime = ev.StartDateTime,
            EndTime = ev.EndTime,
            EventTypeId = ev.EventTypeId,
            VenueId = ev.VenueId,
            SubAreaId = ev.SubAreaId,
            ParentEventId = ev.ParentEventId,
            ImagePath = ev.ImagePath
        };

        context.Event.Add(newEvent);
        await context.SaveChangesAsync();

        return Json(new { success = true, id = newEvent.Id });
    }

    private string GenerateNextName(string currentName)
    {
        if (string.IsNullOrWhiteSpace(currentName)) return "New Event 1";

        var match = System.Text.RegularExpressions.Regex.Match(currentName, @"(\d+)$");

        if (match.Success)
        {
            string numberStr = match.Value;
            if (int.TryParse(numberStr, out int number))
            {
                string baseName = currentName.Substring(0, match.Index).TrimEnd();
                return $"{baseName} {number + 1}";
            }
        }

        // If no trailing number found, append " 1"
        return $"{currentName.TrimEnd()} 1";
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetEventTiming(int id)
    {
        var ev = await context.Event
            .Where(e => e.Id == id)
            .Select(e => new {
                start = e.StartDateTime.ToString("yyyy-MM-ddTHH:mm"),
                end = e.EndTime.ToString("yyyy-MM-ddTHH:mm")
            })
            .FirstOrDefaultAsync();

        if (ev == null) return NotFound();
        return Json(ev);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> MultiSubRename([FromForm] List<int> ids, [FromForm] string NewName)
    {
        try
        {
            var eventsToRename = await context.Event
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();
            var count = 1;
            foreach (var ev in eventsToRename)
            {
                ev.Name = NewName+" ("+count+")";
                count++;
            }

            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Events renamed successfully!" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk renaming events");
            return BadRequest(new { success = false, message = "Error renaming events." });
        }
        
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> getNewestEvents()
    {
        var ev = await context.Event
            .OrderByDescending(e => e.Id)
            .Include(e => e.Venue)
                .ThenInclude(v => v.City)
            .Include(e => e.EventType) 
            .Where(e => e.ParentEventId == null)
            .Take(5)
            .ToListAsync();
            
        if (ev == null) return NotFound();
        return Json(ev);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> HomePage()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await context.EventType
            .Select(c => new {
                id = c.Id,
                name = c.Name, 
            })
            .ToListAsync();

        return Json(categories);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> getRecentEvents()
    {
        var ev = await context.Event
            .OrderBy(e => e.StartDateTime)
            .Include(e => e.Venue)
                .ThenInclude(v => v.City)
            .Include(e => e.EventType) 
            .Where(e => e.ParentEventId == null)
            .Where(e => e.EndTime > DateTime.Now)
            .Take(5)
            .ToListAsync();
            
        if (ev == null) return NotFound();
        return Json(ev);
    }
}