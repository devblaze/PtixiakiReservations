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

        [Authorize(Roles = "Venue,Admin,SuperOrganizer")]
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
        [Authorize(Roles = "Venue,Admin,SuperOrganizer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonSubAreaModel[] subareas)
        {
            if (subareas == null || !subareas.Any())
            {
                return BadRequest(new { error = "No sub-areas provided" });
            }

            var userId = _usermanager.GetUserId(HttpContext.User);

            // Get all valid Venue IDs for this user once to avoid hitting DB in a loop
            var userVenueIds = await _context.Venue
                .Where(v => v.UserId == userId)
                .Select(v => v.Id)
                .ToListAsync();

            foreach (var subarea in subareas)
            {
                if (!userVenueIds.Contains(subarea.VenueId))
                {
                    return Forbid(); // User trying to add areas to someone else's venue
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
                    // NOTE: If you migrate to Layouts, this would be: 
                    // VenueLayoutId = subarea.LayoutId
                };
                _context.Add(newSubArea);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Successfully created all sub-areas" });
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
                subArea.Height = subAreaEdit.Height;
                subArea.Width = subAreaEdit.Width;
                subArea.Top = subAreaEdit.Top;
                subArea.Left = subAreaEdit.Left;



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
                
                return RedirectToAction("VenueSubAreas", "SubAreas", new { venueId = subArea.VenueId });
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

        [HttpPost]
        [Authorize(Roles = "Venue,Admin,SuperOrganizer")]
        public async Task<IActionResult> Duplicate([FromBody] DuplicateSubAreaRequest request)
        {
            // Βρίσκουμε το original layout
            var originalSubArea = await _context.SubArea
                .FirstOrDefaultAsync(sa => sa.Id == request.Id);

            if (originalSubArea == null)
            {
                return NotFound();
            }

            // Δημιουργούμε νέο layout
            var duplicatedSubArea = new SubArea
            {
                AreaName = request.Name,
                Desc = originalSubArea.Desc,
                Width = originalSubArea.Width,
                Height = originalSubArea.Height,
                Top = originalSubArea.Top,
                Left = originalSubArea.Left,
                Rotate = originalSubArea.Rotate,
                VenueId = originalSubArea.VenueId
            };

            _context.SubArea.Add(duplicatedSubArea);

            // Save για να πάρει νέο ID
            await _context.SaveChangesAsync();

            // Παίρνουμε όλα τα seats
            var originalSeats = await _context.Seat
                .Where(s => s.SubAreaId == originalSubArea.Id)
                .ToListAsync();

            // Κάνουμε duplicate τα seats
            foreach (var seat in originalSeats)
            {
                var duplicatedSeat = new Seat
                {
                    Name = seat.Name,
                    X = seat.X,
                    Y = seat.Y,
                    Available = seat.Available,
                    SubAreaId = duplicatedSubArea.Id
                };

                _context.Seat.Add(duplicatedSeat);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }
    }
}
