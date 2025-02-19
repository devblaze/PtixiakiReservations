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

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        [Obsolete]
        public IHostingEnvironment HostingEnviromnet;

        [Obsolete]
        public VenueController(ApplicationDbContext context,
                                IHostingEnvironment hostingEnviromnet, UserManager<ApplicationUser> UserManager, RoleManager<ApplicationRole> roleManager)
        {
            this.roleManager = roleManager;
            this.userManager = UserManager;
            _context = context;
            HostingEnviromnet = hostingEnviromnet;
        }

        // GET: Shops
        public async Task<IActionResult> Index(string city)
        {
            var venues =  _context.Venue.Include(v => v.City).ToList();

            if (!String.IsNullOrEmpty(city))
            {
                var venues2 = _context.Venue.Include(v => v.City).Where(v => v.City.Name == city).ToList();
                return View(venues2);
            }

            return View(venues);
        }

        // GET: Shops/Details/5
        [Authorize(Roles = "Venue")]
        public IActionResult Edit()
        {
            string tmp = userManager.GetUserId(HttpContext.User);

            if (tmp == null)
            {
                ViewBag.Error = string.Format("Something Went Wrong");
                return View("Error");
            }

            var Venue = _context.Venue.Include(v => v.City)
                .Include(v => v.ApplicationUser)
                .FirstOrDefault(s => s.UserId.Equals(tmp));

            if (Venue == null)
            {
                ViewBag.Error = string.Format("Something Went Wrong");
                return View("Error");
            }

            ViewBag.SelectedCity = Venue.City.Name;

            VenueViewModel viewModel = new VenueViewModel
            {
                Name = Venue.Name,
                Address = Venue.Address,
                PostalCode = Venue.PostalCode,
                CityId = Venue.City.Id,
                Phone = Venue.Phone,
                UserId = userManager.GetUserId(HttpContext.User)
            };

            ViewBag.ListOfCity = _context.City.ToList();
            
            return View(viewModel);
        }
        [HttpPost]
        [Obsolete]
        [Authorize(Roles = "Venue")]
        public async Task<IActionResult> Edit(VenueViewModel model)
        {
            Venue venue = _context.Venue.SingleOrDefault(v => v.ApplicationUser.Id == userManager.GetUserId(HttpContext.User));
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
        [Authorize(Roles = "Venue")]
        public IActionResult Create()
        {
            string id = userManager.GetUserId(HttpContext.User);
            var tmp = _context.Venue.Include(v => v.City).Where(s => s.UserId == id).ToList();

            if (tmp.Count != 0)
            {
                ViewBag.Error = string.Format("You can have only 1 Venue");
                return View("Error");
            }
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
                string uniqueFileName = null;
                if(model.Photo != null)
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
                    UserId = userManager.GetUserId(HttpContext.User),
                    imgUrl = uniqueFileName
                   
                };                
                _context.Add(newshop);
                await _context.SaveChangesAsync();
                return RedirectToAction("details",new { id = newshop.Id });
            }
            ViewBag.Error = string.Format("Something Went Wrong");
            return View("Error");          
        }
       
        // GET: Shops/Edit/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                ViewBag.Error = string.Format("Something Went Wrong");
                return View("Error");
            }

            var venue = _context.Venue.Include(v => v.City).FirstOrDefault(v => v.Id == id);

            if (userManager.GetUserId(HttpContext.User) != venue.UserId)
            {
                ViewBag.Error = string.Format("Something Went Wrong");
                return View("Error");
            }
            if (venue == null)
            {
                ViewBag.Error = string.Format("Something Went Wrong");
                return View("Error");
            }
            return View(venue);
        }

        // POST: Shops/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        

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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venue.FindAsync(id);
            _context.Venue.Remove(venue);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VenueExists(int id)
        {
            return _context.Venue.Any(e => e.Id == id);
        }
    }
}
