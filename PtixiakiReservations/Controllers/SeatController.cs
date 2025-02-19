using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using PtixiakiReservations.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mime;
using Newtonsoft.Json;

namespace PtixiakiReservations.Controllers
{
    public class SeatController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext _context;

        public SeatController(ApplicationDbContext context, UserManager<ApplicationUser> UserManager)
        {
            userManager = UserManager;
            _context = context;
        }

        // GET: Table
        public IActionResult Index()
        {

            return View();
        }
        [HttpGet]
        public JsonResult get_data(int? SubAreaId)
        {

            var seats = _context.Seat.Where(s => s.SubAreaId == SubAreaId).ToList();
         
                return Json(seats);
        }     
            public async Task<IActionResult> ListOfMySeats(int? subAreaId)
        {
           
            var id = userManager.GetUserId(HttpContext.User);

            var seats =  _context.Seat
                .Include(s => s.SubArea)
                .Include(r => r.SubArea.Venue).Where(s => s.SubArea.Venue.UserId == id &&  s.SubAreaId == subAreaId);     
            return View(await seats.ToListAsync());
        }
            // GET: Table/Details/5
            public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seat
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
            ViewBag.subAreaId = subAreaId;
            return View("CreateSeatMap");
        }
        // POST: Table/Create
        [HttpPost]
        public async Task<IActionResult> CreateTableMap([FromBody] JsonSeatModel[] seats,int? subAreaId)
        {          
            if (ModelState.IsValid)
            {
                foreach (var s in seats)
                {
                    Seat seat = new Seat
                    {
                        Name = s.Name,
                        X = s.left,
                        Y = s.top,
                        SubAreaId = (int)subAreaId,
                        Available = true

                    };
                    _context.Add(seat);
                }
            }
                
            await _context.SaveChangesAsync();           
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

            var seat = await _context.Seat.FindAsync(id);
            if (seat == null)
            {
                return NotFound();
            }    
            ViewData["shopID"] = new SelectList(_context.Venue, "ID", "ID", seat.SubAreaId);
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
                    _context.Update(Seat);
                    await _context.SaveChangesAsync();
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
            ViewData["VenueId"] = new SelectList(_context.Venue, "ID", "ID", Seat.SubAreaId);
            return View(Seat);
        }
        public async Task<IActionResult> ChangeAvailable(int ID, bool Flag)
        {

            Seat Seat = _context.Seat.FirstOrDefault(t => t.Id == ID);
            if (Seat != null)
            {
                Seat.Available = Flag;
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(Seat);
                        await _context.SaveChangesAsync();
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


        // GET: Table/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seat.FirstOrDefaultAsync(m => m.Id == id);

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
            var seat = await _context.Seat.FindAsync(id);
            var res = _context.Reservation
                .Where(r => r.SeatId == seat.Id).ToList();
            if (res.Count != 0)
            {
                foreach (var r in res)
                {
                    _context.Reservation.Remove(r);
                }
            }
            _context.Seat.Remove(seat);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ListOfMySeats));
        }

        private bool SeatExists(int id)
        {
            return _context.Seat.Any(e => e.Id == id);
        }
    }
}
