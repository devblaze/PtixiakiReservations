using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Net;

namespace PtixiakiReservations.Controllers;

public class SeatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    : Controller
{
    // GET: Table
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public JsonResult get_data(int? SubAreaId, int? eventId)
    {
        if (SubAreaId == null) return Json(new { });

        // First get all seats for this subarea
        var seats = context.Seat
            .Where(s => s.SubAreaId == SubAreaId)
            .ToList(); // Get the seat entities first

        // If eventId is provided, get all seat IDs that are already reserved for this event
        List<int> reservedSeatIds = new List<int>();
        if (eventId.HasValue)
        {
            reservedSeatIds = context.Reservation
                .Where(r => r.EventId == eventId.Value)
                .Select(r => r.SeatId)
                .ToList();
        }

        // Create a new list of objects with the merged data
        var seatViewModels = seats.Select(seat => new
        {
            id = seat.Id,
            name = seat.Name,
            x = (float)seat.X,
            y = (float)seat.Y,
            available = seat.Available &&
                        !reservedSeatIds.Contains(seat.Id) // Check both the seat's availability and if it's reserved
        }).ToList();

        return Json(seatViewModels);
    }

    public async Task<IActionResult> ListOfMySeats(int? subAreaId)
    {
        var id = userManager.GetUserId(HttpContext.User);

        var seats = context.Seat
            .Include(s => s.SubArea)
            .Include(r => r.SubArea.Venue).Where(s => s.SubArea.Venue.UserId == id && s.SubAreaId == subAreaId);
        return View(await seats.ToListAsync());
    }

    // GET: Table/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var seat = await context.Seat
            .Include(t => t.SubArea.Venue)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (seat == null)
        {
            return NotFound();
        }

        return View(seat);
    }

    // GET: Table/Create
    public IActionResult Create(int? subAreaId)
    {
        if (subAreaId == null)
            return NotFound();

        var existingSeats = context.Seat.Any(s => s.SubAreaId == subAreaId);

        ViewData["SubAreaId"] = subAreaId.Value;
        ViewData["HasExistingSeats"] = existingSeats;

        return View("CreateSeatMap");
    }

// POST: Table/Create
    [HttpPost]
    public async Task<IActionResult> CreateTableMap([FromBody] JsonSeatModel[] seats, int subAreaId)
    {
        try
        {
            if (seats == null || !seats.Any())
            {
                return BadRequest("No seat data provided");
            }

            if (subAreaId <= 0)
            {
                return BadRequest("Invalid sub area ID");
            }

            // Verify the sub area exists
            var subArea = await context.SubArea.FindAsync(subAreaId);
            if (subArea == null)
            {
                return NotFound("Sub area not found");
            }

            // Check if there are existing seats for this subarea
            var existingSeats = await context.Seat
                .Where(s => s.SubAreaId == subAreaId)
                .ToListAsync();

            // If this is an edit operation (existing seats found), remove them all first
            if (existingSeats.Any())
            {
                context.Seat.RemoveRange(existingSeats);
                await context.SaveChangesAsync();
            }

            // Now add the new seats
            foreach (var s in seats)
            {
                Seat seat = new Seat
                {
                    Name = s.Name,
                    X = s.x,
                    Y = s.y,
                    SubAreaId = subAreaId,
                    Available = true
                };
                context.Add(seat);
            }

            await context.SaveChangesAsync();
            return Ok(new { success = true, message = "Seats created successfully" });
        }
        catch (Exception ex)
        {
            // Log the error
            return StatusCode(500, new { success = false, message = "An error occurred while saving seats", error = ex.Message });
        }
    }
    
    // GET: Table/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var seat = await context.Seat.FindAsync(id);
        if (seat == null)
        {
            return NotFound();
        }
        ViewData["shopID"] = new SelectList(context.Venue, "ID", "ID", seat.SubAreaId);
        return View(seat);
    }

    // POST: Table/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,ReservationId,VenueId")] Seat Seat)
    {
        if (id != Seat.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                context.Update(Seat);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SeatExists(Seat.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["VenueId"] = new SelectList(context.Venue, "ID", "ID", Seat.SubAreaId);
        return View(Seat);
    }

    public async Task<IActionResult> ChangeAvailable(int ID, bool Flag)
    {
        Seat Seat = context.Seat.FirstOrDefault(t => t.Id == ID);
        if (Seat != null)
        {
            Seat.Available = Flag;
            if (ModelState.IsValid)
            {
                try
                {
                    context.Update(Seat);
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SeatExists(Seat.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
        return RedirectToAction("ListOfMySeats", "Seat", new { subAreaId = Seat.SubAreaId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMultipleSeats([FromBody] DeleteMultipleSeatsRequest request)
    {
        var seatsToRemove = await context.Seat
            .Where(s => request.seatNames.Contains(s.Name) && s.SubAreaId == request.subAreaId)
            .ToListAsync();

        if (!seatsToRemove.Any())
            return Json(new { success = false, message = "No matching seats found to delete." });

        context.Seat.RemoveRange(seatsToRemove);
        await context.SaveChangesAsync();

        return Json(new { success = true });
    }
    
    // GET: Table/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var seat = await context.Seat.FirstOrDefaultAsync(m => m.Id == id);

        if (seat == null)
        {
            return NotFound();
        }

        return View(seat);
    }

    // POST: Table/Delete/5
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var seat = await context.Seat.FindAsync(id);
        var res = context.Reservation
            .Where(r => r.SeatId == seat.Id).ToList();
        if (res.Count != 0)
        {
            foreach (var r in res)
            {
                context.Reservation.Remove(r);
            }
        }
        context.Seat.Remove(seat);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(ListOfMySeats));
    }

    private bool SeatExists(int id)
    {
        return context.Seat.Any(e => e.Id == id);
    }
}

public class DeleteMultipleSeatsRequest
{
    public List<string> seatNames { get; set; }
    public int subAreaId { get; set; }
}