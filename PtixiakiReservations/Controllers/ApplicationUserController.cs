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

namespace PtixiakiReservations.Controllers
{
    public class ApplicationUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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
            var user= await _userManager.FindByIdAsync(id);

            return View(user);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRoleAction(String id,String Role)
        {
            IdentityResult result;
            var user = await _userManager.FindByIdAsync(id);
            var flag=await _userManager.IsInRoleAsync(user,Role);
            if (!flag)
            {
                 result = await _userManager.AddToRoleAsync(user, Role);
            }
            else
            {
                result = await _userManager.RemoveFromRoleAsync(user, Role);          
            }
            if (!result.Succeeded)
            {
                return NotFound();
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
    }
}