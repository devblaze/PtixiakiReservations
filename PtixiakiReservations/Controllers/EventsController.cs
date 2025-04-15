using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;
using PtixiakiReservations.Services;

namespace PtixiakiReservations.Controllers;

public class EventsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IElasticSearch elasticSearchService)
    : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;

    // GET: Events
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> EventsForToday(string city)
    {
        var today = DateTime.Today;
        
        var eventsForToday = await context.Event
            .Include(e => e.Venue)
            .ThenInclude(v => v.City)
            .Where(e => e.StartDateTime.Date == today)
            .ToListAsync();

        return View(eventsForToday);
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

    // Search events
    [HttpGet]
    public async Task<IActionResult> SearchEvents(string query)
    {
        var results = await elasticSearchService.SearchAsync<Event>(query, "events");
        return View(results);
    }
}