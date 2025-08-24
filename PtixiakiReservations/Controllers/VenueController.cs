using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;


namespace PtixiakiReservations.Controllers
{
    public class VenueController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        [Obsolete] public readonly IHostingEnvironment HostingEnviromnet;

        [Obsolete]
        public VenueController(ApplicationDbContext context,
            IHostingEnvironment hostingEnviromnet, UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
            HostingEnviromnet = hostingEnviromnet;
        }

        // GET: Shops
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string city)
        {
            var venues = await _context.Venue.Include(v => v.City).ToListAsync();

            if (city is null) return View(venues);

            var venues2 = await _context.Venue.Include(v => v.City).Where(v => v.City.Name == city).ToListAsync();
            return View(venues2);
        }

        [Authorize(Roles = "Venue")]
        public async Task<IActionResult> MyVenues()
        {
            string userId = _userManager.GetUserId(HttpContext.User);
            var venues = await _context.Venue
                .Include(v => v.City)
                .Where(v => v.UserId == userId)
                .ToListAsync();

            // Get subarea counts for each venue
            var subAreaCounts = new Dictionary<int, int>();
            var imagePaths = new Dictionary<int, string>();
            
            foreach (var venue in venues)
            {
                var count = await _context.SubArea.CountAsync(sa => sa.VenueId == venue.Id);
                subAreaCounts[venue.Id] = count;
                
                // Add image path validation
                imagePaths[venue.Id] = GetImagePath(venue.imgUrl);
            }

            ViewBag.SubAreaCounts = subAreaCounts;
            ViewBag.ImagePaths = imagePaths;

            // Count events for all venues managed by this user
            var eventCount = await _context.Event
                .Where(e => venues.Select(v => v.Id).Contains(e.VenueId))
                .CountAsync();

            ViewBag.EventCount = eventCount;

            return View(venues);
        }

        // GET: Venue/Edit/5
        [Authorize(Roles = "Admin,Venue")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var venue = await _context.Venue
                .Include(v => v.City)
                .Include(v => v.ApplicationUser)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venue == null)
            {
                return NotFound();
            }

            // Allow venue managers to edit their own venues, and admins to edit any venue
            if (!User.IsInRole("Admin") && _userManager.GetUserId(HttpContext.User) != venue.UserId)
            {
                return Forbid();
            }

            ViewBag.SelectedCity = venue.City.Name;

            VenueViewModel viewModel = new VenueViewModel
            {
                Id = venue.Id,  // Add Id to the view model
                Name = venue.Name,
                Address = venue.Address,
                PostalCode = venue.PostalCode,
                CityId = venue.CityId,
                Phone = venue.Phone,
                UserId = venue.UserId
            };

            ViewBag.ListOfCity = _context.City.ToList();

            return View(viewModel);
        }

        [HttpPost]
        [Obsolete]
        [Authorize(Roles = "Venue,Admin")]
        public async Task<IActionResult> Edit(VenueViewModel model)
        {
            Venue venue =
                _context.Venue.SingleOrDefault(v => v.ApplicationUser.Id == _userManager.GetUserId(HttpContext.User));
            if (model == null || venue == null)
            {
                ViewBag.Error = string.Format("You dont have a Venue yet or something went wrong on your edit");
                return View("Error");
            }
            if (ModelState.IsValid)
            {
                string uniqueFileName = null;
                try
                {
                    if (model.Photo == null)
                    {
                        uniqueFileName = _context.Venue.SingleOrDefault(s => s.Id == venue.Id).imgUrl;
                    }
                    else
                    {
                        string uploadsFolder = Path.Combine(HostingEnviromnet.WebRootPath, "images");
                        uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Photo.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        model.Photo.CopyTo(new FileStream(filePath, FileMode.Create));
                    }

                    venue.Name = model.Name;
                    venue.Phone = model.Phone;
                    venue.PostalCode = model.PostalCode;
                    if (uniqueFileName != null)
                    {
                        venue.imgUrl = uniqueFileName;
                    }
                    var t = model.CityId;
                    venue.CityId = model.CityId;
                    venue.Address = model.Address;

                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return RedirectToAction("details", new { id = venue.Id });
        }

        // GET: Shops/Create
        [Authorize(Roles = "Venue,Admin")]
        public IActionResult Create()
        {
            string id = _userManager.GetUserId(HttpContext.User);
            var tmp = _context.Venue.Include(v => v.City).Where(s => s.UserId == id).ToList();

            // if (tmp.Count != 0)
            // {
            //     ViewBag.Error = string.Format("You can have only 1 Venue");
            //     return View("Error");
            // }
            ViewBag.ListOfCity = _context.City.ToList();
            return View();
        }

        // POST: Shops/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete]
        public async Task<IActionResult> Create(VenueViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);

                string uniqueFileName = null;
                if (model.Photo != null)
                {
                    string uploadsFolder = Path.Combine(HostingEnviromnet.WebRootPath, "images");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Photo.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    model.Photo.CopyTo(new FileStream(filePath, FileMode.Create));
                }

                Venue newshop = new Venue
                {
                    Name = model.Name,
                    Address = model.Address,
                    CityId = model.CityId,
                    PostalCode = model.PostalCode,
                    Phone = model.Phone,
                    UserId = userId,
                    imgUrl = uniqueFileName
                };
                _context.Add(newshop);
                await _context.SaveChangesAsync();
                return RedirectToAction("details", new { id = newshop.Id });
            }
            ViewBag.Error = string.Format("Something Went Wrong");
            return View("Error");
        }

        // GET: Venue/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var venue = await _context.Venue
                .Include(v => v.City)
                .FirstOrDefaultAsync(v => v.Id == id);
        
            if (venue == null)
            {
                return NotFound();
            }

            // Allow venue managers to view their own venues, and admins to view any venue
            if (!User.IsInRole("Admin") && _userManager.GetUserId(HttpContext.User) != venue.UserId)
            {
                return Forbid();
            }

            ViewBag.ImagePath = GetImagePath(venue.imgUrl);
        
            return View(venue);
        }

        // GET: Shops/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var venues = await _context.Venue
                .FirstOrDefaultAsync(m => m.Id == id);
            if (venues == null)
            {
                return NotFound();
            }

            return View(venues);
        }

        // POST: Shops/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venue.FindAsync(id);
            _context.Venue.Remove(venue);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        [Authorize(Roles = "Venue,Admin")]
        public IActionResult GetVenuesForUser()
        {
            string userId = _userManager.GetUserId(HttpContext.User);
            var venues = _context.Venue
                .Where(v => v.UserId == userId)
                .Select(v => new { id = v.Id, name = v.Name })
                .ToList();
    
            return Json(venues);
        }

        private bool VenueExists(int id)
        {
            return _context.Venue.Any(e => e.Id == id);
        }
    
        private string GetImagePath(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return "~/images/image.jpg";
            
            var imagePath = Path.Combine(HostingEnviromnet.WebRootPath, "images", imageUrl);
            
            if (System.IO.File.Exists(imagePath))
                return $"~/images/{imageUrl}";
            else
                return "~/images/image.jpg";
        }
    }
}