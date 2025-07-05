using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            // Get user with City included
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.City)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var reservations = await _context.Reservation
                .Include(r => r.Event)
                .Include(r => r.Seat)
                .Include(r => r.Seat.SubArea)
                .Include(r => r.Seat.SubArea.Venue)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.Date)
                .Take(5)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                User = user,
                Roles = roles,
                RecentReservations = reservations
            };

            return View(model);
        }

        // GET: /Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Load city name if city exists
            if (user.CityId.HasValue)
            {
                var city = await _context.City.FindAsync(user.CityId.Value);
                ViewBag.CityName = city?.Name;
            }

            var model = new ProfileEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CityId = user.CityId,
                Address = user.Address,
                PostalCode = user.PostalCode
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.CityId = model.CityId;
            user.Address = model.Address;
            user.PostalCode = model.PostalCode;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Profile/Reservations
        public async Task<IActionResult> Reservations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var reservations = await _context.Reservation
                .Include(r => r.Event)
                .Include(r => r.Seat)
                .Include(r => r.Seat.SubArea)
                .Include(r => r.Seat.SubArea.Venue)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return View(reservations);
        }

// GET: /Profile/RequestVenueManager
        public IActionResult RequestVenueManager()
        {
            return View(new VenueManagerRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestVenueManager(VenueManagerRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user already has pending request
            if (user.HasRequestedVenueManagerRole && user.VenueManagerRequestStatus == "Pending")
            {
                ModelState.AddModelError(string.Empty, "You already have a pending request to become a venue manager.");
                return View(model);
            }

            // Check if user is already a venue manager
            if (await _userManager.IsInRoleAsync(user, "VenueManager"))
            {
                ModelState.AddModelError(string.Empty, "You are already a venue manager.");
                return View(model);
            }

            // Update user with request details
            user.HasRequestedVenueManagerRole = true;
            user.VenueManagerRequestDate = DateTime.UtcNow;
            user.VenueManagerRequestReason = model.Reason;
            user.VenueManagerRequestStatus = "Pending";

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Your request to become a venue manager has been submitted and is pending approval.";
            return RedirectToAction(nameof(Index));
        }
    }
}