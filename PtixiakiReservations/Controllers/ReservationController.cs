using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;

namespace PtixiakiReservations.Controllers
{
    [Authorize]
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        public ReservationController(ApplicationDbContext context, UserManager<ApplicationUser> _userManager,RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            userManager = _userManager;
            this.roleManager = roleManager;
        }

        // GET: Reservations        
        public async Task<IActionResult> Index(bool flag,string sortOrder)
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

                res = _context.Reservation.Include(r => r.Event)
                    .Include(r => r.ApplicationUser)
                    .Include(r => r.Seat)
                    .Include(r => r.Seat.SubArea)
                    .Include(r => r.Seat.SubArea.Venue)
                    .Where(r => r.Seat.SubArea.Venue.UserId == userManager.GetUserId(HttpContext.User) 
                    ).ToList();


                res = res.Where(r => r.Date.Subtract((DateTime)date) <= span && r.Date.Subtract((DateTime)date) >= span2).ToList();
                res = FilterRes(sortOrder, res);
                return View(res);
            }


            res = FilterRes(sortOrder, res);
            return  View(res);
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
            public  JsonResult isFree(int EventId,int SubAreaId, DateTime ResDate ,TimeSpan Duration)
        {
            var subArea = _context.SubArea.SingleOrDefault(s => s.Id == SubAreaId);
            int numOfSeats = _context.Seat.Where(s => s.SubAreaId == SubAreaId).Count();

            int[] seatIds = new int[numOfSeats*2];

            
            DateTime NowDateTime = DateTime.Now;
            var reservations =  _context.Reservation.Include(r => r.Event).
                Include(r => r.Seat)
                .Include(r => r.Seat.SubArea.Venue)
                .Where(r => r.Seat.SubArea.Id== subArea.Id && r.EventId == EventId).ToList();
            int i = 0;
            if (NowDateTime.Date == ResDate.Date)
            {
                var seatsUnAvailable = _context.Seat.Where(s => s.SubAreaId == subArea.Id && s.Available == false).ToList();
                foreach (var s in seatsUnAvailable)
                {
                    seatIds[i++] = s.Id;
                }
            }                
                reservations = reservations
                     .Where(res => ResDate <= res.Date
                    && res.Date <= ResDate.AddMinutes(Duration.TotalMinutes)
                    || ResDate <= res.Date.AddMinutes(res.Duration.TotalMinutes)
                    && res.Date.AddMinutes(res.Duration.TotalMinutes) >= ResDate.AddMinutes(Duration.TotalMinutes)).ToList();
           
            foreach (var r in reservations)
            {
                seatIds[i++] = r.SeatId;
            }

            return  Json(seatIds);
        }    
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservations = await _context.Reservation
                .Include(r => r.ApplicationUser)
                .Include(r => r.Event)
                .Include(r => r.Seat)
                .Include(r => r.Seat.SubArea)
                .Include(r => r.Seat.SubArea.Venue)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (reservations == null)
            {
                return NotFound();
            }

            return View(reservations);
        }
       
        public IActionResult Create(int? VenueId, int? EventId)
        {
            var ev = _context.Event.SingleOrDefault(e => e.Id == EventId);
            if (ev == null)
            {
                ViewBag.Error = string.Format("Something Went Wrong");
                return View("Error");
            }
            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> MakeRes([FromBody] int[] res , int EventId , TimeSpan Duration , DateTime ResDate)
        {
            
            if (ModelState.IsValid)
            {
           //   DateTime date = (DateTime)ResDate;

                foreach (var r in res)
                {
                    Reservation reservation = new Reservation
                    {

                        SeatId = r,
                        UserId = userManager.GetUserId(HttpContext.User),
                        EventId = EventId,
                        Duration = Duration,
                        Date = ResDate
                    };

                   _context.Add(reservation);

               }
                await _context.SaveChangesAsync();
            }
            else{
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(Response.StatusCode);
            }

            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(Response.StatusCode); ; 
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

        private bool ReservationsExists(int id)
        {
            return _context.Reservation.Any(e => e.ID == id);
        }
    }
}
