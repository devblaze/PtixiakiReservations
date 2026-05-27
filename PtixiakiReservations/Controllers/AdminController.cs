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
                PendingRequests = await _context.Users
                    .Where(u => (u.HasRequestedVenueManagerRole && u.VenueManagerRequestStatus == "Pending") ||
                        (u.HasRequestedEventManagerRole && u.EventManagerRequestStatus == "Pending")||
                        (u.HasRequestedSuperOrganizerRole && u.SuperOrganizerRequestStatus == "Pending"))
                    .CountAsync()
            };

            return View(model);
        }

        // GET: /Admin/RoleRequests
        public async Task<IActionResult> RoleRequests()
        {
            // Fetch users with ANY pending role request
            var pendingRequests = await _context.Users
                .Where(u => 
                    (u.HasRequestedVenueManagerRole && u.VenueManagerRequestStatus == "Pending") ||
                    (u.HasRequestedEventManagerRole && u.EventManagerRequestStatus == "Pending") ||
                    (u.HasRequestedSuperOrganizerRole && u.SuperOrganizerRequestStatus == "Pending")
                )
                
                .OrderByDescending(u => u.VenueManagerRequestDate ?? u.EventManagerRequestDate ?? u.SuperOrganizerRequestDate)
                .ToListAsync();

            return View(pendingRequests);
}

        // POST: /Admin/ApproveRoleRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRoleRequest(string userId, string roleType)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            bool wasApproved = false;
            string newRole = string.Empty;

            switch (roleType)
            {
                case "Venue":
                    if (user.HasRequestedVenueManagerRole && user.VenueManagerRequestStatus == "Pending")
                    {
                        user.VenueManagerRequestStatus = "Approved";
                        newRole = "Venue";
                        wasApproved = true;
                    }
                    break;

                case "Event":
                    if (user.HasRequestedEventManagerRole && user.EventManagerRequestStatus == "Pending")
                    {
                        user.EventManagerRequestStatus = "Approved";
                        newRole = "Event";
                        wasApproved = true;
                    }
                    break;

                case "SuperOrganizer":
                    if (user.HasRequestedSuperOrganizerRole && user.SuperOrganizerRequestStatus == "Pending")
                    {
                        user.SuperOrganizerRequestStatus = "Approved";
                        newRole = "SuperOrganizer";
                        wasApproved = true;
                    }
                    break;

                default:
                    return BadRequest("Invalid role type submitted.");
            }

            if (wasApproved && !string.IsNullOrEmpty(newRole))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                await _userManager.AddToRoleAsync(user, newRole);
                
                await _userManager.UpdateAsync(user);
                
                TempData["SuccessMessage"] = $"The {roleType} request for {user.FirstName} {user.LastName} has been approved. Previous roles were removed.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not approve the request. It may have already been processed or cancelled.";
            }
            return RedirectToAction(nameof(RoleRequests));
        }

        // POST: /Admin/RejectRoleRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRoleRequest(string userId, string roleType)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            bool wasRejected = false;

            switch (roleType)
            {
                case "Venue":
                    if (user.HasRequestedVenueManagerRole && user.VenueManagerRequestStatus == "Pending")
                    {
                        user.VenueManagerRequestStatus = "Rejected";
                        wasRejected = true;
                    }
                    break;

                case "Event":
                    if (user.HasRequestedEventManagerRole && user.EventManagerRequestStatus == "Pending")
                    {
                        user.EventManagerRequestStatus = "Rejected";
                        wasRejected = true;
                    }
                    break;

                case "SuperOrganizer":
                    if (user.HasRequestedSuperOrganizerRole && user.SuperOrganizerRequestStatus == "Pending")
                    {
                        user.SuperOrganizerRequestStatus = "Rejected";
                        wasRejected = true;
                    }
                    break;

                default:
                    return BadRequest("Invalid role type submitted.");
            }

            
            if (wasRejected)
            {
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"The {roleType} request for {user.FirstName} {user.LastName} has been rejected.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not reject the request. It may have already been processed or cancelled.";
            }

            return RedirectToAction("RoleRequests");
        }
    }
}