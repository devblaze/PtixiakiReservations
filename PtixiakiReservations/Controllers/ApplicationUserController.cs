using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Seeders;
using PtixiakiReservations.Services;

namespace PtixiakiReservations.Controllers
{
    public class ApplicationUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        // GET: ApplicationUser
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();

            return View(users);
        }

        // GET: ApplicationUser/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: ApplicationUser/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(String id)
        {
            var user = await _userManager.FindByIdAsync(id);

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRoleAction(string id, string Role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }


            bool alreadyHasRole = await _userManager.IsInRoleAsync(user, Role);

            // 3. Wipe the slate clean (remove all current roles)
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest("Failed to remove existing roles.");
                }
            }

            string roleToAssign = alreadyHasRole ? "User" : Role;

            // 5. Apply the new role
            var addResult = await _userManager.AddToRoleAsync(user, roleToAssign);
            if (!addResult.Succeeded)
            {
                return BadRequest($"Failed to assign the {roleToAssign} role.");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "ApplicationUser");
        }

        public async Task<IActionResult> SeedAdminUser()
        {
            try
            {
                // Call the overloaded method directly using the injected services
                await ApplicationDbSeed.SeedAsync(_userManager, _roleManager);

                // Return success response
                return Ok("Admin seeding completed successfully.");
            }
            catch (Exception ex)
            {
                // Catch and return any errors during the seeding process
                return BadRequest($"Seeding failed: {ex.Message}");
            }
        }

        [HttpGet]
        public JsonResult SearchCities(string term)
        {
            var cities = _context.City
                .Where(c => c.Name.Contains(term))
                .Select(c => new { id = c.Id, value = c.Name })
                .Take(10)
                .ToList();

            return Json(cities);
        }

        public class Toggle2FaRequest
        {
            public string Password { get; set; }
            public bool Enable { get; set; } // True = Turn On, False = Turn Off
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle2Fa([FromBody] Toggle2FaRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return BadRequest(new { message = "Incorrect password." });
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, request.Enable);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                
                return Ok(new { message = request.Enable ? "2FA Enabled" : "2FA Disabled" });
            }

            return BadRequest(new { message = "Failed to update settings." });
        }

        // 1. This generates the code and "wakes up" the modal
        [HttpPost]
        public async Task<IActionResult> GenerateEmailConfirmationCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            string subject = "Your EventSphere Security Code";
            string message = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2>Security Verification</h2>
                    <p>You requested a verification code.</p>
                    <p>Your code is: <strong style='font-size: 24px; color: #4F46E5;'>{code}</strong></p>
                    <p style='font-size: 12px; color: #666;'>If you did not request this code, please ignore this email.</p>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, message);
                
                return Ok(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EMAIL ERROR: {ex.Message}");
                
                return StatusCode(500, new { message = "Failed to send verification email. Please try again." });
            }
        }

        // 2. This checks the code from the modal
        [HttpPost]
        public async Task<IActionResult> ConfirmEmailCode(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code);

            if (isValid)
            {
                user.EmailConfirmed = true; // Sets the 'emailconfirmed' column in Postgres
                await _userManager.UpdateAsync(user);
                return Ok(new { success = true });
            }

            return BadRequest("Invalid code.");
        }
    }
}
