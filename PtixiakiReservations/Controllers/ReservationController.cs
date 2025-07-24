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
    ApplicationDbContext _context,
    UserManager<ApplicationUser> _userManager,
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
                _context.Reservation.Include(r => r.Event)
                    .Include(r => r.ApplicationUser)
                    .Include(r => r.Seat)
                    .Include(r => r.Seat.SubArea)
                    .Include(r => r.Seat.SubArea.Venue)
                    .Where(r => r.Seat.SubArea.Venue.UserId == _userManager.GetUserId(HttpContext.User)).ToList();

            res = FilterRes(sortOrder, res);
            return View(res);
        }

        string id = _userManager.GetUserId(HttpContext.User);
        var user = await _userManager.FindByIdAsync(id);
        var tmp1 = await _userManager.GetRolesAsync(user);

        if (tmp1.Contains("Venue"))
        {
            DateTime date = DateTime.Now;
            TimeSpan span = TimeSpan.FromHours(14);
            TimeSpan span2 = TimeSpan.FromHours(-14);

            res = _context.Reservation.Include(r => r.Event)
                .Include(r => r.ApplicationUser)
                .Include(r => r.Seat)
                .Include(r => r.Seat.SubArea)
                .Include(r => r.Seat.SubArea.Venue)
                .Where(r => r.Seat.SubArea.Venue.UserId == _userManager.GetUserId(HttpContext.User)
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
        var subArea = _context.SubArea.SingleOrDefault(s => s.Id == SubAreaId);
        int numOfSeats = _context.Seat.Where(s => s.SubAreaId == SubAreaId).Count();

        int[] seatIds = new int[numOfSeats * 2];

        DateTime NowDateTime = DateTime.Now;
        var reservations = _context.Reservation.Include(r => r.Event).Include(r => r.Seat)
            .Include(r => r.Seat.SubArea.Venue)
            .Where(r => r.Seat.SubArea.Id == subArea.Id && r.EventId == EventId).ToList();
    
        int i = 0;
        if (NowDateTime.Date == ResDate.Date)
        {
            var seatsUnAvailable =
                _context.Seat.Where(s => s.SubAreaId == subArea.Id && s.Available == false).ToList();
            foreach (var s in seatsUnAvailable)
            {
                seatIds[i++] = s.Id;
            }
        }
    
        // Fixed overlap detection logic
        reservations = reservations
            .Where(res => 
                // Check for time overlap: Two time periods overlap if one starts before the other ends
                ResDate < res.Date.Add(res.Duration) && 
                ResDate.Add(Duration) > res.Date
            ).ToList();

        foreach (var r in reservations)
        {
            seatIds[i++] = r.SeatId;
        }

        return Json(seatIds);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var reservations = await _context.Reservation
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
        
        var ev = _context.Event.SingleOrDefault(e => e.Id == eventId);
        
        return View(ev);
    }

    [HttpPost]
    public async Task<IActionResult> MakeRes([FromBody] ReservationRequestViewModel model)
    {
        try
        {
            // Validate that the event is not in the past
            var eventToReserve = await _context.Event.FindAsync(model.EventId);
            if (eventToReserve == null)
            {
                return BadRequest("Event not found");
            }

            // Ensure we're comparing dates in the same timezone
            var today = DateTime.Today;

            // Check if the event date is in the past
            if (eventToReserve.StartDateTime.Date < today)
            {
                return BadRequest("Cannot make reservations for past events");
            }

            // Also verify the reservation date is not in the past
            if (model.ResDate.Date < today)
            {
                return BadRequest("Cannot make reservations with a past date");
            }

           // Ensure the ResDate uses the event's date but preserves the time from the reservation
            model.ResDate = CombineDateWithTime(eventToReserve.StartDateTime, model.ResDate);

            static DateTime CombineDateWithTime(DateTime dateSource, DateTime timeSource)
            {
                return new DateTime(
                    dateSource.Year,
                    dateSource.Month,
                    dateSource.Day,
                    timeSource.Hour,
                    timeSource.Minute,
                    timeSource.Second,
                    dateSource.Kind);
            }

            // Additional validation: Check if trying to reserve for a different date than the event
            if (model.ResDate.Date != eventToReserve.StartDateTime.Date)
            {
                return BadRequest($"Reservations must be made for the event date: {eventToReserve.StartDateTime.Date:yyyy-MM-dd}");
            }

            // Continue with the existing logic to make the reservation
            var userId = _userManager.GetUserId(HttpContext.User);
            var user = await _userManager.FindByIdAsync(userId);

            foreach (var seatId in model.SeatIds)
            {
                Reservation res = new Reservation
                {
                    Duration = model.Duration,
                    UserId = userId,
                    ApplicationUser = user,
                    Date = model.ResDate,
                    SeatId = seatId,
                    EventId = model.EventId
                };
                _context.Reservation.Add(res);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // GET: Reservations/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reservations = await _context.Reservation.FindAsync(id);
        if (reservations == null)
        {
            return NotFound();
        }
        ViewData["userId"] = new SelectList(_context.Users, "Id", "Id", reservations.UserId);
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
                _context.Update(reservations);
                await _context.SaveChangesAsync();
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
        ViewData["userId"] = new SelectList(_context.Users, "Id", "Id", reservations.UserId);
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

        var reservations = await _context.Reservation
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
        var reservations = await _context.Reservation.FindAsync(id);
        _context.Reservation.Remove(reservations);
        await _context.SaveChangesAsync();
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

        var eventDetails = await _context.Event
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

    public async Task<IActionResult> SelectSeats(int eventId, int subAreaId, string duration, string resDate)
    {
        var subArea = await _context.SubArea
            .Include(s => s.Venue)
            .FirstOrDefaultAsync(s => s.Id == subAreaId);

        if (subArea == null)
        {
            return NotFound();
        }

        var @event = await _context.Event.FindAsync(eventId);
        if (@event == null)
        {
            return NotFound();
        }

        ViewData["EventId"] = eventId;
        ViewData["SubAreaId"] = subAreaId;
        ViewData["Duration"] = duration;
        ViewData["ResDate"] = resDate;
        ViewData["VenueName"] = subArea.Venue.Name;
        ViewData["SubAreaName"] = subArea.AreaName;

        return View();
    }

    private bool ReservationsExists(int id)
    {
        return _context.Reservation.Any(e => e.ID == id);
    }
}