using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var dashboardStats = new
        {
            VenueCount = await context.Venue.CountAsync(),
            EventCount = await context.Event.CountAsync(),
            SubAreaCount = await context.SubArea.CountAsync(),
            ReservationCount = await context.Reservation.CountAsync()
        };

        return View(dashboardStats);
    }

    // Events Management
    public async Task<IActionResult> Events()
    {
        var events = await context.Event
            .Include(e => e.Venue)
            .Include(e => e.EventType)
            .ToListAsync();

        return View(events);
    }

    public async Task<IActionResult> EditEvent(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @event = await context.Event
            .Include(e => e.Venue)
            .Include(e => e.EventType)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (@event == null)
        {
            return NotFound();
        }

        ViewData["EventTypeId"] = new SelectList(context.EventType, "Id", "Name", @event.EventTypeId);
        ViewData["VenueId"] = new SelectList(context.Venue, "Id", "Name", @event.VenueId);

        return View(@event);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id,
        [Bind("Id,Name,StartDateTime,EndTime,EventTypeId,VenueId")] Event @event)
    {
        if (id != @event.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                context.Update(@event);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Events));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(@event.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        ViewData["EventTypeId"] = new SelectList(context.EventType, "Id", "Name", @event.EventTypeId);
        ViewData["VenueId"] = new SelectList(context.Venue, "Id", "Name", @event.VenueId);

        return View(@event);
    }

    public async Task<IActionResult> DeleteEvent(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @event = await context.Event
            .Include(e => e.Venue)
            .Include(e => e.EventType)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (@event == null)
        {
            return NotFound();
        }

        return View(@event);
    }

    [HttpPost, ActionName("DeleteEvent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEventConfirmed(int id)
    {
        var @event = await context.Event.FindAsync(id);
        if (@event != null)
        {
            context.Event.Remove(@event);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Events));
    }

    private bool EventExists(int id)
    {
        return context.Event.Any(e => e.Id == id);
    }
}