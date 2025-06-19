using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.Requests;

namespace PtixiakiReservations.Controllers;

[Authorize]
public class ReservationController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager)
    : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;

    // GET: Reservations        
    public async Task<IActionResult> Index(bool flag, string sortOrder)
    {
        List<Reservation> res = new List<Reservation>();

        ViewBag.Flag = flag;

        ViewData["NameSortParm"] = sortOrder == "LastName" ? "LastName_desc" : "LastName";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "Date_desc" : "Date";
        ViewData["VenueSortParm"] = sortOrder == "Venue" ? "Venue_desc" : "Venue";
        ViewData["AreaNameSortParm"] = sortOrder == "AreaName" ? "AreaName_desc" : "AreaName";
        ViewData["EventSortParm"] = sortOrder == "Event" ? "Event_desc" : "Event";


        if (flag == true)
        {
            res =
                context.Reservation.Include(r => r.Event)
                    .Include(r => r.ApplicationUser)
                    .Include(r => r.Seat)
                    .Include(r => r.Seat.SubArea)
                    .Include(r => r.Seat.SubArea.Venue)
                    .Where(r => r.Seat.SubArea.Venue.UserId == userManager.GetUserId(HttpContext.User)).ToList();

            res = FilterRes(sortOrder, res);
            return View(res);
        }

        string id = userManager.GetUserId(HttpContext.User);
        var user = await userManager.FindByIdAsync(id);
        var tmp1 = await userManager.GetRolesAsync(user);

        if (tmp1.Contains("Venue"))
        {
            DateTime date = DateTime.Now;
            TimeSpan span = TimeSpan.FromHours(14);
            TimeSpan span2 = TimeSpan.FromHours(-14);

            res = context.Reservation.Include(r => r.Event)
                .Include(r => r.ApplicationUser)
                .Include(r => r.Seat)
                .Include(r => r.Seat.SubArea)
                .Include(r => r.Seat.SubArea.Venue)
                .Where(r => r.Seat.SubArea.Venue.UserId == userManager.GetUserId(HttpContext.User)
                ).ToList();


            res = res.Where(
                r => r.Date.Subtract((DateTime)date) <= span && r.Date.Subtract((DateTime)date) >= span2).ToList();
            res = FilterRes(sortOrder, res);
            return View(res);
        }


        res = FilterRes(sortOrder, res);
        return View(res);
    }

    public List<Reservation> FilterRes(string sortOrder, List<Reservation> res)
    {
        switch (sortOrder)
        {
            case "LastName":
                res = res.OrderBy(r => r.ApplicationUser.LastName).ToList();
                break;
            case "Date":
                res = res.OrderBy(r => r.Date).ToList();
                break;
            case "Event":
                res = res.OrderBy(s => s.Event.Name).ToList();
                break;
            case "Venue":
                res = res.OrderBy(s => s.Seat.SubArea.Venue.Name).ToList();
                break;
            case "AreaName":
                res = res.OrderBy(s => s.Seat.SubArea.AreaName).ToList();
                break;
            case "LastName_desc":
                res = res.OrderByDescending(r => r.ApplicationUser.LastName).ToList();
                break;
            case "Date_desc":
                res = res.OrderByDescending(r => r.Date).ToList();
                break;
            case "Event_desc":
                res = res.OrderByDescending(s => s.Event.Name).ToList();
                break;
            case "Venue_desc":
                res = res.OrderByDescending(s => s.Seat.SubArea.Venue.Name).ToList();
                break;
            case "AreaName_desc":
                res = res.OrderByDescending(s => s.Seat.SubArea.AreaName).ToList();
                break;
            default:
                res = res.OrderBy(s => s.Date).ToList();
                break;
        }
        return res;
    }

    public JsonResult isFree(int EventId, int SubAreaId, DateTime ResDate, TimeSpan Duration)
    {
        var subArea = context.SubArea.SingleOrDefault(s => s.Id == SubAreaId);
        int numOfSeats = context.Seat.Where(s => s.SubAreaId == SubAreaId).Count();

        int[] seatIds = new int[numOfSeats * 2];


        DateTime NowDateTime = DateTime.Now;
        var reservations = context.Reservation.Include(r => r.Event).Include(r => r.Seat)
            .Include(r => r.Seat.SubArea.Venue)
            .Where(r => r.Seat.SubArea.Id == subArea.Id && r.EventId == EventId).ToList();
        int i = 0;
        if (NowDateTime.Date == ResDate.Date)
        {
            var seatsUnAvailable =
                context.Seat.Where(s => s.SubAreaId == subArea.Id && s.Available == false).ToList();
            foreach (var s in seatsUnAvailable)
            {
                seatIds[i++] = s.Id;
            }
        }
        reservations = reservations
            .Where(res => ResDate <= res.Date
                          && res.Date <= ResDate.AddMinutes(Duration.TotalMinutes)
                          || ResDate <= res.Date.AddMinutes(res.Duration.TotalMinutes)
                          && res.Date.AddMinutes(res.Duration.TotalMinutes) >=
                          ResDate.AddMinutes(Duration.TotalMinutes)).ToList();

        foreach (var r in reservations)
        {
            seatIds[i++] = r.SeatId;
        }

        return Json(seatIds);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var reservations = await context.Reservation
            .Include(r => r.ApplicationUser)
            .Include(r => r.Event)
            .Include(r => r.Seat)
            .Include(r => r.Seat.SubArea)
            .Include(r => r.Seat.SubArea.Venue)
            .FirstOrDefaultAsync(m => m.ID == id);
        
        if (reservations is null) return NotFound();

        return View(reservations);
    }

    public IActionResult Create(int? eventId)
    {
        if (eventId is null) return NotFound();
        
        var ev = context.Event.SingleOrDefault(e => e.Id == eventId);
        
        return View(ev);
    }

    [HttpPost]
    public async Task<IActionResult> MakeRes([FromBody] ReservationRequestViewModel request)
    {
        if (request == null || request.SeatIds == null || !request.SeatIds.Any())
        {
            return BadRequest("No seats selected");
        }

        try
        {
            string userId = userManager.GetUserId(HttpContext.User);

            // Validate the event exists
            var eventEntity = await context.Event.FindAsync(request.EventId);
            if (eventEntity == null)
            {
                return BadRequest("Event not found");
            }

            // Get the selected seats
            var seatIds = request.SeatIds;
            var seats = await context.Seat
                .Where(s => seatIds.Contains(s.Id))
                .ToListAsync();

            if (seats.Count != seatIds.Length)
            {
                return BadRequest("One or more selected seats do not exist");
            }

            // Check if any of the seats are unavailable (not just for this event, but in general)
            var unavailableSeats = seats.Where(s => !s.Available).ToList();
            if (unavailableSeats.Any())
            {
                return BadRequest(
                    $"Seat(s) {string.Join(", ", unavailableSeats.Select(s => s.Name))} are not available");
            }

            // Check for existing reservations for this event
            var existingReservations = await context.Reservation
                .Where(r => r.EventId == request.EventId && seatIds.Contains(r.SeatId))
                .ToListAsync();

            if (existingReservations.Any())
            {
                var reservedSeatIds = existingReservations.Select(r => r.SeatId).ToList();
                var reservedSeats = seats.Where(s => reservedSeatIds.Contains(s.Id)).Select(s => s.Name);
                return BadRequest(
                    $"Seat(s) {string.Join(", ", reservedSeats)} have already been reserved for this event");
            }

            // Create reservations
            foreach (var seatId in seatIds)
            {
                var reservation = new Reservation
                {
                    SeatId = seatId,
                    UserId = userId,
                    EventId = request.EventId,
                    Duration = request.Duration,
                    Date = request.ResDate
                };

                context.Add(reservation);
            }

            await context.SaveChangesAsync();
            return Ok("Reservations created successfully");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating reservations: {ex.Message}");
        }
    }

    // GET: Reservations/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reservations = await context.Reservation.FindAsync(id);
        if (reservations == null)
        {
            return NotFound();
        }
        ViewData["userId"] = new SelectList(context.Users, "Id", "Id", reservations.UserId);
        return View(reservations);
    }

    // POST: Reservations/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,people,userId,shopId,date")] Reservation reservations)
    {
        if (id != reservations.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                context.Update(reservations);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationsExists(reservations.ID))
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
        ViewData["userId"] = new SelectList(context.Users, "Id", "Id", reservations.UserId);
        //  ViewData["shopId"] = new SelectList(_context.Shops, "ID", "ID", reservations.shopId);
        return View(reservations);
    }

    // GET: Reservations1/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reservations = await context.Reservation
            .Include(r => r.ApplicationUser)
            // .Include(r => r.shop)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (reservations == null)
        {
            return NotFound();
        }

        return View(reservations);
    }

    // POST: Reservations1/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var reservations = await context.Reservation.FindAsync(id);
        context.Reservation.Remove(reservations);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    
    public IActionResult SelectSeats(int eventId, int venueId)
    {
        // Pass EventId and VenueId to the Select Seats page
        ViewData["EventId"] = eventId;
        ViewData["VenueId"] = venueId;

        return View("SelectSeats");
    }

    // GET: Reservation/ReserveSeats/5
    public async Task<IActionResult> ReserveSeats(int? eventId)
    {
        if (eventId == null)
        {
            return NotFound();
        }

        var eventDetails = await context.Event
            .Include(e => e.Venue)
            .Include(e => e.Venue.City)
            .Include(e => e.EventType)
            .FirstOrDefaultAsync(m => m.Id == eventId);

        if (eventDetails == null)
        {
            return NotFound();
        }

        ViewData["EventId"] = eventId;
        ViewData["VenueId"] = eventDetails.VenueId;

        return View(eventDetails);
    }

    private bool ReservationsExists(int id)
    {
        return context.Reservation.Any(e => e.ID == id);
    }
}