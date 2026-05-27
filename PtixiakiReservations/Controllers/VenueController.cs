﻿using System;
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

        [Authorize(Roles = "Admin,Venue,SuperOrganizer")]
        public async Task<IActionResult> MyVenues(string filter = "mine", int page = 1, int pageSize = 12)
        {
            string userId = _userManager.GetUserId(HttpContext.User);
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            var query = _context.Venue
                .Include(v => v.City)
                .Include(v => v.VenueCategory)
                    .ThenInclude(vc => vc.EventType)
                .AsQueryable();

            if (filter != "all") 
            {
                query = query.Where(v => v.UserId == userId);
            }

            // Calculate global stats for the filtered set before applying pagination
            int totalCount = await query.CountAsync();
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.CityCount = await query.Select(v => v.CityId).Distinct().CountAsync();

            // Fetch only the venues for the current page
            var venues = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var subAreaCounts = new Dictionary<int, int>();
            var imagePaths = new Dictionary<int, string>();
            
            foreach (var venue in venues)
            {
                var count = await _context.SubArea.CountAsync(sa => sa.VenueId == venue.Id);
                subAreaCounts[venue.Id] = count;

                imagePaths[venue.Id] = GetImagePath(venue.imgUrl);
            }

            ViewBag.SubAreaCounts = subAreaCounts;
            ViewBag.ImagePaths = imagePaths;

            // Global event count for this specific filter
            var eventCount = await _context.Event
                .Where(e => query.Any(v => v.Id == e.VenueId))
                .CountAsync();

            ViewBag.EventCount = eventCount;

            return View(venues);
        }

        // GET: Venue/Edit/5
        [Authorize(Roles = "Admin,Venue,SuperOrganizer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var venue = await _context.Venue
                .Include(v => v.City)
                .Include(v => v.ApplicationUser)
                .Include(v => v.VenueCategory)
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

            var selectedEventTypeIds = venue.VenueCategory?
                .Where(vc => vc.CategoryId.HasValue)
                .Select(vc => vc.CategoryId.Value)
                .ToList() ?? new List<int>();

            VenueViewModel viewModel = new VenueViewModel
            {
                Id = venue.Id,
                Name = venue.Name,
                Address = venue.Address,
                PostalCode = venue.PostalCode,
                CityId = venue.CityId,
                Phone = venue.Phone,
                VenueUrl = venue.VenueUrl,
                SocialMediaUrl = venue.SocialMediaUrl,
                UserId = venue.UserId,
                SelectedEventTypeIds = selectedEventTypeIds
            };

            PopulateVenueFormLists(venue.CityId, selectedEventTypeIds);
            ViewBag.ImagePath = GetImagePath(venue.imgUrl);

            return View(viewModel);
        }

        [HttpPost]
        [Obsolete]
        [Authorize(Roles = "Admin,Venue,SuperOrganizer")]
        public async Task<IActionResult> Edit(VenueViewModel model)
        {
            if (model == null)
            {
                ViewBag.Error = string.Format("You dont have a Venue yet or something went wrong on your edit");
                return View("Error");
            }

            var venue = await _context.Venue
                .Include(v => v.VenueCategory)
                .FirstOrDefaultAsync(v => v.Id == model.Id);

            if (venue == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && _userManager.GetUserId(HttpContext.User) != venue.UserId)
            {
                return Forbid();
            }

            var selectedEventTypeIds = model.SelectedEventTypeIds?
                .Distinct()
                .ToList() ?? new List<int>();

            if (ModelState.IsValid)
            {
                string uniqueFileName = null;
                try
                {
                    if (model.Photo == null)
                    {
                        uniqueFileName = venue.imgUrl;
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
                    venue.VenueUrl = model.VenueUrl;
                    venue.SocialMediaUrl = model.SocialMediaUrl;
                    if (uniqueFileName != null)
                    {
                        venue.imgUrl = uniqueFileName;
                    }
                    venue.CityId = model.CityId;
                    venue.Address = model.Address;

                    if (venue.VenueCategory != null && venue.VenueCategory.Any())
                    {
                        _context.VenueCategory.RemoveRange(venue.VenueCategory);
                    }

                    if (selectedEventTypeIds.Any())
                    {
                        var categoriesToAttach = selectedEventTypeIds.Select(typeId => new VenueCategory
                        {
                            VenueId = venue.Id,
                            CategoryId = typeId
                        }).ToList();

                        _context.VenueCategory.AddRange(categoriesToAttach);
                    }
                    else
                    {
                        _context.VenueCategory.Add(new VenueCategory
                        {
                            VenueId = venue.Id,
                            CategoryId = null
                        });
                    }

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
            else
            {
                PopulateVenueFormLists(model.CityId, selectedEventTypeIds);
                ViewBag.ImagePath = GetImagePath(venue.imgUrl);
                return View(model);
            }
            TempData["SuccessMessage"] = "Venue updated successfully";
            return RedirectToAction("details", new { id = venue.Id });
        }

        [Authorize(Roles = "Admin,Venue,SuperOrganizer")]
        public IActionResult Create()
        {
            string id = _userManager.GetUserId(HttpContext.User);
            
            ViewBag.ListOfCity = _context.City.ToList();
            ViewBag.EventTypes = new MultiSelectList(_context.EventType.ToList(), "Id", "Name");
            
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
                    VenueUrl = model.VenueUrl,
                    SocialMediaUrl = model.SocialMediaUrl,
                    UserId = userId,
                    imgUrl = uniqueFileName
                };
                _context.Add(newshop);
                await _context.SaveChangesAsync();

                if (model.SelectedEventTypeIds != null && model.SelectedEventTypeIds.Any())
                {
                    var categoriesToAttach = model.SelectedEventTypeIds.Select(typeId => new VenueCategory
                    {
                        VenueId = newshop.Id,
                        CategoryId = typeId
                    }).ToList();

                    _context.VenueCategory.AddRange(categoriesToAttach);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.VenueCategory.Add(new VenueCategory
                    {
                        VenueId = newshop.Id,
                        CategoryId = null 
                    });
                }
                return RedirectToAction("details", new { id = newshop.Id });
            }
            ViewBag.ListOfCity = new SelectList(_context.City.ToList(), "Id", "Name", model.CityId);
            ViewBag.EventTypes = new MultiSelectList(_context.EventType.ToList(), "Id", "Name", model.SelectedEventTypeIds);
            return View(model);
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
                .Include(v => v.VenueCategory)           // YOU NEED THIS
                    .ThenInclude(vc => vc.EventType)
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
        [Authorize(Roles = "Admin,Venue,SuperOrganizer")]
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

        private void PopulateVenueFormLists(int selectedCityId, IEnumerable<int> selectedEventTypeIds = null)
        {
            ViewBag.ListOfCity = new SelectList(_context.City.ToList(), "Id", "Name", selectedCityId);
            ViewBag.EventTypes = new MultiSelectList(
                _context.EventType.ToList(),
                "Id",
                "Name",
                selectedEventTypeIds ?? Enumerable.Empty<int>());
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
