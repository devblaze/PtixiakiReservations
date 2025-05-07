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
    public JsonResult get_data(int? SubAreaId)
    {
        if (SubAreaId == null) return Json(new { });

        var seats = context.Seat
            .Where(s => s.SubAreaId == SubAreaId)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                x = (float)s.X, // Convert decimal to float for JS compatibility
                y = (float)s.Y, // Convert decimal to float for JS compatibility
                available = s.Available
            }).ToList();

        return Json(seats);
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
    public async Task<IActionResult> CreateTableMap([FromBody] JsonSeatModel[] seats, int? subAreaId)
    {
        if (ModelState.IsValid && subAreaId.HasValue)
        {
            // Check if there are existing seats for this subarea
            var existingSeats = await context.Seat
                .Where(s => s.SubAreaId == subAreaId.Value)
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
                    X = s.left,
                    Y = s.top,
                    SubAreaId = subAreaId.Value,
                    Available = true
                };
                context.Add(seat);
            }

            await context.SaveChangesAsync();
        }

        Response.StatusCode = (int)HttpStatusCode.OK;
        return Json(Response.StatusCode);
    }

    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.

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
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
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
