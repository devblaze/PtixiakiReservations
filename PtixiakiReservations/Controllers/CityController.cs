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
    public class CityController : Controller
    {        private readonly ApplicationDbContext _context;

        public CityController(ApplicationDbContext context)
        {
            _context = context;
        }

        //GET: returns all cities
        public async Task<IActionResult> ManageCities()
        {
            var cities = await _context.City.ToListAsync();
            return View(cities);
        }

        public IActionResult CreateCity()
        {
            return View();
        }

        //POST: creates a city
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCity(string CityName)
        {
            if (!string.IsNullOrWhiteSpace(CityName))
            {
                _context.City.Add(new City {Name = CityName});
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageCities));}
            return View();
        }

        //POST: deletes a city
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var city = await _context.City.FindAsync(id);

            if (city != null)
            {
                _context.City.Remove(city);
                await _context.SaveChangesAsync();

            }
            return RedirectToAction(nameof(ManageCities));
        }

        //GET: returns specific city to edit
        public async Task<IActionResult> EditCity(int CityId)
        {
            var city = await _context.City.FindAsync(CityId);

            if (city == null)
            {
                return NotFound();

            }

            return View(city);
        }

        //POST: edits the city
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCity(int CityId, string CityName)
        {

            var city = await _context.City.FindAsync(CityId);

            if(city!=null && !string.IsNullOrWhiteSpace(CityName))
            {
                city.Name = CityName;
                _context.Update(city);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageCities));
            }
            else
            {
                return NoContent();
            }
        }

    }
    }