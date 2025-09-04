using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;

namespace PtixiakiReservations.Controllers
{
    public class SubAreasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _usermanager;
        public SubAreasController(ApplicationDbContext context, UserManager<ApplicationUser> usermanager)
        {
            _context = context;
            _usermanager = usermanager;
        }

        [Authorize(Roles = "Venue")]
        public async Task<IActionResult> Index()
        {
            var subAreas = await _context.SubArea
                .Select(sa => new
                {
                    sa.Id,
                    sa.AreaName,
                    sa.Desc,
                    HasSeats = _context.Seat.Any(seat => seat.SubAreaId == sa.Id)
                })
                .ToListAsync();

            ViewBag.SubAreas = subAreas;
            return View();
        }

        public async Task<IActionResult> ChooseSubArea(int venueId, int eventId, string duration, string resDate)
        {
            var venue = await _context.Venue.FindAsync(venueId);
            if (venue == null)
            {
                return NotFound();
            }

            ViewData["EventId"] = eventId;
            ViewData["VenueId"] = venueId;
            ViewData["Duration"] = duration;
            ViewData["ResDate"] = resDate;

            return View(venue);
        }
        
        // GET: SubAreas/Details/5
        public async Task<IActionResult> Details(int? id, int? venueId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subArea = await _context.SubArea
                .Include(s => s.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (subArea == null)
            {
                return NotFound();
            }

            // Pass venueId to the view for proper back navigation
            ViewBag.VenueId = venueId ?? subArea.VenueId;
            ViewBag.VenueName = subArea.Venue?.Name;

            return View(subArea);
        }

        // GET: SubAreas/Create
        [Authorize(Roles = "Venue")]
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonSubAreaModel[] subareas)
        {
            if (subareas == null || !subareas.Any())
            {
                ViewBag.Error = "Something went wrong";
                return View("Error");
            }

            // Get user ID
            var userId = _usermanager.GetUserId(HttpContext.User);

            foreach (var subarea in subareas)
            {
                // Validate that the venue belongs to the current user
                var venue = await _context.Venue
                    .FirstOrDefaultAsync(v => v.Id == subarea.VenueId && v.UserId == userId);

                if (venue == null)
                {
                    return BadRequest("Invalid venue selection");
                }

                SubArea newSubArea = new SubArea
                {
                    AreaName = subarea.AreaName,
                    Height = subarea.Height,
                    Width = subarea.Width,
                    Rotate = subarea.Rotate,
                    Top = subarea.Top,
                    Left = subarea.Left,
                    VenueId = subarea.VenueId
                };
                _context.Add(newSubArea);
            }
            await _context.SaveChangesAsync();

            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(Response.StatusCode);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromVenue([FromBody]JsonSubAreaModel[] subareas)
        {
            if(subareas == null)
            {
                ViewBag.Error = "Something went wrong";
                return View("Error");
            }
            var venue = await _context.Venue.FirstOrDefaultAsync(v => v.ApplicationUser.Id == _usermanager.GetUserId(HttpContext.User));
            foreach (var subarea in subareas)
            {
                SubArea newSubArea = new SubArea
                {
                    AreaName = subarea.AreaName,
                    Height = subarea.Height,
                    Width = subarea.Width,
                    Rotate = subarea.Rotate,
                    Top = subarea.Top,
                    Left = subarea.Left,
                    VenueId = venue.Id
                };
                _context.Add(newSubArea);
            }
            await _context.SaveChangesAsync();

            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(Response.StatusCode);
        }

        // GET: SubAreas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subArea = await _context.SubArea.FindAsync(id);
            if (subArea == null)
            {
                return NotFound();
            }
            ViewData["VenueId"] = new SelectList(_context.Venue, "Id", "Id", subArea.VenueId);
            return View(subArea);
        }

        // POST: SubAreas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id,SubArea subAreaEdit)
        {
            var subArea = _context.SubArea.SingleOrDefault(s => s.Id == id);
            if (id != subArea.Id)
            {
                return NotFound();
            }
           
            if (ModelState.IsValid)
            {
                subArea.AreaName = subAreaEdit.AreaName;
                subArea.Desc = subAreaEdit.Desc;
                try
                {                   
                    _context.Update(subArea);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubAreaExists(subArea.Id)) return NotFound();

                    throw;
                }
                
                return RedirectToAction(nameof(Index));
            }
            ViewData["VenueId"] = new SelectList(_context.Venue, "Id", "Id", subArea.VenueId);
            return View(subArea);
        }

        // GET: SubAreas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subArea = await _context.SubArea
                .Include(s => s.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (subArea == null)
            {
                return NotFound();
            }

            return View(subArea);
        }

        // POST: SubAreas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seats = _context.Seat.Where(s => s.SubAreaId == id).ToList();
            if (seats != null)
            {
                foreach (var s in seats)
                {
                    var result = new SeatController(_context, _usermanager).DeleteConfirmed(s.Id);
                }
            }
            var subArea = await _context.SubArea.FindAsync(id);
            _context.SubArea.Remove(subArea);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: SubAreas/VenueSubAreas/5
        public async Task<IActionResult> VenueSubAreas(int venueId)
        {
            if (venueId == 0)
            {
                return NotFound();
            }

            var venue = await _context.Venue.FindAsync(venueId);
            if (venue == null)
            {
                return NotFound();
            }

            var subAreas = await _context.SubArea
                .Where(sa => sa.VenueId == venueId)
                .ToListAsync();

            ViewBag.VenueName = venue.Name;
            ViewBag.VenueId = venueId;

            return View(subAreas);
        }

        [HttpGet]
        public JsonResult GetSubAreas(int venueId)
        {
            var subAreas = _context.SubArea
                .Where(sa => sa.VenueId == venueId)
                .Select(sa => new { id = sa.Id, areaName = sa.AreaName, desc = sa.Desc })
                .ToList();

            return Json(subAreas);
        }

        private bool SubAreaExists(int id)
        {
            return _context.SubArea.Any(e => e.Id == id);
        }
    }
}
