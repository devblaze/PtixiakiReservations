using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /Admin/Index (Admin Dashboard)
        public async Task<IActionResult> Index()
        {
            var model = new
            {
                VenueCount = await _context.Venue.CountAsync(),
                EventCount = await _context.Event.CountAsync(),
                SubAreaCount = await _context.SubArea.CountAsync(),
                ReservationCount = await _context.Reservation.CountAsync(),
                PendingVenueManagerRequests = await _context.Users
                    .Where(u => u.HasRequestedVenueManagerRole && u.VenueManagerRequestStatus == "Pending")
                    .CountAsync()
            };

            return View(model);
        }

        // GET: /Admin/VenueManagerRequests
        public async Task<IActionResult> VenueManagerRequests()
        {
            var pendingRequests = await _context.Users
                .Where(u => u.HasRequestedVenueManagerRole && u.VenueManagerRequestStatus == "Pending")
                .OrderBy(u => u.VenueManagerRequestDate)
                .ToListAsync();

            var recentlyProcessed = await _context.Users
                .Where(u => u.HasRequestedVenueManagerRole && 
                           (u.VenueManagerRequestStatus == "Approved" || u.VenueManagerRequestStatus == "Rejected") &&
                           u.VenueManagerRequestDate > DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(u => u.VenueManagerRequestDate)
                .ToListAsync();

            ViewBag.RecentlyProcessed = recentlyProcessed;
            return View(pendingRequests);
        }

        // POST: /Admin/ApproveVenueManagerRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVenueManagerRequest(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.HasRequestedVenueManagerRole && user.VenueManagerRequestStatus == "Pending")
            {
                // Update request status
                user.VenueManagerRequestStatus = "Approved";
                await _userManager.UpdateAsync(user);

                // Add user to VenueManager role
                await _userManager.AddToRoleAsync(user, "VenueManager");

                TempData["SuccessMessage"] = $"Request for {user.Email} has been approved.";
            }

            return RedirectToAction(nameof(VenueManagerRequests));
        }

        // POST: /Admin/RejectVenueManagerRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVenueManagerRequest(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.HasRequestedVenueManagerRole && user.VenueManagerRequestStatus == "Pending")
            {
                // Update request status
                user.VenueManagerRequestStatus = "Rejected";
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = $"Request for {user.Email} has been rejected.";
            }

            return RedirectToAction(nameof(VenueManagerRequests));
        }
    }
}