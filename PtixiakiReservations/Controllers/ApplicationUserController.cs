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

namespace PtixiakiReservations.Controllers
{
    public class ApplicationUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> _userManager, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            userManager = _userManager;
            this.roleManager = roleManager;
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
            var user= await userManager.FindByIdAsync(id);

            return View(user);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRoleAction(String id,String Role)
        {
            IdentityResult result;
            var user = await userManager.FindByIdAsync(id);
            var flag=await userManager.IsInRoleAsync(user,Role);
            if (!flag)
            {
                 result = await userManager.AddToRoleAsync(user, Role);
            }
            else
            {
                result = await userManager.RemoveFromRoleAsync(user, Role);          
            }
            if (!result.Succeeded)
            {
                return NotFound();
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "ApplicationUser");           
        }
 
    }
}