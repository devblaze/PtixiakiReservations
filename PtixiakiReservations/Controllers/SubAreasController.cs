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
            var applicationDbContext = _context.SubArea.Include(s => s.Venue);
            return View(await applicationDbContext.ToListAsync());
        }
        public IActionResult ChooseSubArea()
        {
            return View();
        }
            public JsonResult GetSubAreas(int? venueId)
        {
            if(venueId == 0)
            {
                return null;
            }
            var subAreas = _context.SubArea.Where(s => s.VenueId == venueId).ToList();

            return Json(subAreas);
        }
        // GET: SubAreas/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: SubAreas/Create
        [Authorize(Roles = "Venue")]
        public IActionResult Create()
        {
            // var venue = _context.Venue.SingleOrDefault(v => v.UserId == _usermanager.GetUserId(HttpContext.User));
            var venue = _context.Venue.Find(2);
            if (venue is null)
            {
                ViewBag.Error = "You need to Create Venue First";
                return View("Error");
            }
            
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody]JsonSubAreaModel[] subAreas)
        {
            if(subAreas == null)
            {
                ViewBag.Error = "Something went wrong";
                return View("Error");
            }
            var venue = _context.Venue.SingleOrDefault(v => v.ApplicationUser.Id == _usermanager.GetUserId(HttpContext.User));
            foreach (var sA in subAreas)
            {
                SubArea newSubArea = new SubArea
                {
                    AreaName = sA.AreaName,
                    Height = sA.Height,
                    Width = sA.Width,
                    Rotate = sA.Rotate,
                    Top = sA.Top,
                    Left = sA.Left,
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
                    if (!SubAreaExists(subArea.Id))
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

        private bool SubAreaExists(int id)
        {
            return _context.SubArea.Any(e => e.Id == id);
        }
    }
}
