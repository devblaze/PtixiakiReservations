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
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using PtixiakiReservations.Services;

namespace PtixiakiReservations.Controllers;

[Authorize]
public class ReservationController(
    ApplicationDbContext _context,
    UserManager<ApplicationUser> _userManager,
    RoleManager<ApplicationRole> roleManager,
    IEmailService _emailService)
    : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;

    // GET: Reservations        
    public async Task<IActionResult> Index(bool flag, string sortOrder)
    {
        ViewBag.Flag = flag;

        ViewData["NameSortParm"] = sortOrder == "LastName" ? "LastName_desc" : "LastName";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "Date_desc" : "Date";
        ViewData["VenueSortParm"] = sortOrder == "Venue" ? "Venue_desc" : "Venue";
        ViewData["AreaNameSortParm"] = sortOrder == "AreaName" ? "AreaName_desc" : "AreaName";
        ViewData["EventSortParm"] = sortOrder == "Event" ? "Event_desc" : "Event";

        string id = _userManager.GetUserId(HttpContext.User);

        if (string.IsNullOrEmpty(id))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // UPDATE PAST RESERVATIONS
        var allReservations = await _context.Reservation.ToListAsync();

        foreach (var reservation in allReservations)
        {
            if (!reservation.IsPastReservation &&
                reservation.Date.Add(reservation.Duration) < DateTime.Now)
            {
                reservation.IsPastReservation = true;
            }
        }

        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(id);
        var roles = await _userManager.GetRolesAsync(user);

        List<Reservation> res;

        res = await _context.Reservation
            .Include(r => r.Event)
            .Include(r => r.ApplicationUser)
            .Include(r => r.Seat)
            .ThenInclude(s => s.SubArea)
            .ThenInclude(sa => sa.Venue)
            .Where(r => r.UserId == id)
            .ToListAsync();

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

        // First filter reservations by the specific date and event
        var reservations = _context.Reservation.Include(r => r.Event).Include(r => r.Seat)
            .Include(r => r.Seat.SubArea.Venue)
            .Where(r => r.Seat.SubArea.Id == subArea.Id && r.EventId == EventId)
            .Where(r => r.Date.Date == ResDate.Date) // Filter by specific date for multi-day events
            .ToList();

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

        // Check for time overlap on the same date
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
            var eventToReserve = await _context.Event.FindAsync(model.EventId);

            if (eventToReserve == null)
            {
                return BadRequest("Event not found");
            }

            var today = DateTime.Today;

            if (eventToReserve.StartDateTime.Date < today)
            {
                return BadRequest("Cannot make reservations for past events");
            }

            if (model.ResDate.Date < today)
            {
                return BadRequest("Cannot make reservations with a past date");
            }

            var userId = _userManager.GetUserId(HttpContext.User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (model == null)
            {
                return BadRequest("Reservation model is null");
            }

            if (model.SeatIds == null || !model.SeatIds.Any())
            {
                return BadRequest("No seats selected");
            }

            if (user == null)
            {
                return Unauthorized();
            }

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

            // ================= EMAIL DATA =================

            var seatIds = model.SeatIds.ToList();

            var selectedSeatNames = await _context.Seat
                .Where(s => seatIds.Contains(s.Id))
                .Select(s => s.Name)
                .ToListAsync();

            var eventEntity = await _context.Event
                .AsNoTracking()
                .Include(e => e.Venue)
                .ThenInclude(v => v.City)
                .Include(e => e.SubArea)
                .FirstOrDefaultAsync(e => e.Id == model.EventId);

            var subject = "Reservation Confirmation";

            var seatsText = string.Join(", ", selectedSeatNames);

            var message = $@"
            <div style='font-family:Arial;padding:20px;background:#f4f4f4;'>
                <div style='background:white;padding:30px;border-radius:10px;max-width:700px;margin:auto;'>

                    <h2 style='color:#2563eb;'>Reservation Confirmation</h2>

                    <p>Hello {user.UserName},</p>

                    <p>Your reservation has been completed successfully.</p>

                    <hr/>

                    <p><strong>Event:</strong> {eventEntity?.Name}</p>
                    <p><strong>Venue:</strong> {eventEntity?.Venue?.Name}</p>
                    <p><strong>City:</strong> {eventEntity?.Venue?.City?.Name}</p>
                    <p><strong>Area:</strong> {eventEntity?.SubArea?.AreaName}</p>

                    <p><strong>Date:</strong> {model.ResDate:dd/MM/yyyy}</p>
                    <p><strong>Time:</strong> {model.ResDate:HH:mm}</p>
                    <p><strong>Duration:</strong> {model.Duration}</p>

                    <p><strong>Seats:</strong> {seatsText}</p>

                    <br/>

                    <p>Thank you for your reservation.</p>

                </div>
            </div>";

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    subject,
                    message
                );
            }
            catch
            {
                // Optional logging
                // Δεν αποτυγχάνει το reservation αν αποτύχει το email
            }

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
            .Include(e => e.SubArea)
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

    [HttpPost]
    public async Task<IActionResult> SubmitReview(int reservationId, bool attended, string review, int rating)
    {
        var reservation = await _context.Reservation
            .FirstOrDefaultAsync(r => r.ID == reservationId);

        if (reservation != null)
        {
            reservation.Attended = attended;

            if (attended)
            {
                reservation.Review = review;
                reservation.Rating = rating;
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("reservation/{ID}/ics")]
    public IActionResult GetIcs(int id)
    {
        var reservation = _context.Reservation.FirstOrDefault(r => r.ID == id);

        var calendar = new Ical.Net.Calendar();

        var bookedEvent = _context.Event.FirstOrDefault(e => e.Id ==reservation.EventId);
        if (bookedEvent == null)
        {
            return BadRequest("Event not found");
        }

        var eventVenue = _context.Venue.FirstOrDefault(v => v.Id ==bookedEvent.VenueId);
        if (eventVenue == null)
        {
            return BadRequest("Venue not found");
        }

        var bookedSeat = _context.Seat.FirstOrDefault(s => s.Id ==reservation.SeatId);

        var venueCity = _context.City.FirstOrDefault(c => c.Id ==eventVenue.CityId);

        var resDesc = (bookedEvent.Name + " at " + eventVenue.Name + " (" + eventVenue.Address + ", " + venueCity.Name + ")" + ".");

        if (bookedSeat != null) 
        {
            resDesc += (" Seat: " + bookedSeat.Name);
        }

        var mapQuery = Uri.EscapeDataString(eventVenue.Name + ", " + eventVenue.Address + ", " + venueCity.Name);

        var e = new Ical.Net.CalendarComponents.CalendarEvent
        {
            Summary = bookedEvent.Name,
            Description = resDesc,
            Location= $"https://www.google.com/maps/search/?api=1&query={mapQuery}",
            Start = new Ical.Net.DataTypes.CalDateTime(reservation.Date),
            End = new Ical.Net.DataTypes.CalDateTime(reservation.Date.AddHours(1)),
            Uid = Guid.NewGuid().ToString(),
            Created = new Ical.Net.DataTypes.CalDateTime(DateTime.UtcNow)
        };

        calendar.Events.Add(e);

        var serializer = new Ical.Net.Serialization.CalendarSerializer();
        var icalString = serializer.SerializeToString(calendar);

        var bytes = System.Text.Encoding.UTF8.GetBytes(icalString);

        return File(bytes, "text/calendar", "reservation.ics");
    }
    [HttpPost]
    public async Task<IActionResult> SaveReview(int reservationId, string review, int rating)
    {
        var userId = _userManager.GetUserId(User);

        var reservation = await _context.Reservation
            .FirstOrDefaultAsync(r => r.ID == reservationId && r.UserId == userId);

        if (reservation == null)
            return NotFound();

        if (reservation.Attended != true)
        {
            return BadRequest();
        }

        reservation.Review = review;
        reservation.Rating = rating;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> MarkAttendance(int reservationId, bool attended)
    {
        var userId = _userManager.GetUserId(User);

        var reservation = await _context.Reservation
            .FirstOrDefaultAsync(r => r.ID == reservationId && r.UserId == userId);

        if (reservation == null)
            return NotFound();

        reservation.Attended = attended;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    private bool ReservationsExists(int id) 
    { 
        return _context.Reservation.Any(e => e.ID == id); 
    }

}