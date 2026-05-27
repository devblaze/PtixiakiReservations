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
    public class EventTypeController : Controller
    {        private readonly ApplicationDbContext _context;

        public EventTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        //GET: return all event types
        public async Task<IActionResult> ManageEventType()
        {
            var et = await _context.EventType.ToListAsync();
            return View(et);
        }

        public IActionResult CreateEventType()
        {
            return View();
        }

        //POST: creatin event type
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEventType(string ETName)
        {
            if (!string.IsNullOrWhiteSpace(ETName))
            {
                _context.EventType.Add(new EventType {Name = ETName});
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageEventType));}
            return View();
        }

        //POST: delete event type
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventType(int id)
        {
            var et = await _context.EventType.FindAsync(id);

            if (et != null)
            {
                _context.EventType.Remove(et);
                await _context.SaveChangesAsync();

            }
            return RedirectToAction(nameof(ManageEventType));
        }

        //GET: return selected event type to edit
        public async Task<IActionResult> EditEventType(int ETId)
        {
            var et = await _context.EventType.FindAsync(ETId);

            if (et == null)
            {
                return NotFound();

            }

            return View(et);
        }

        //POST: edit event type
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEventType(int ETId, string ETName)
        {

            var et = await _context.EventType.FindAsync(ETId);

            if(et!=null && !string.IsNullOrWhiteSpace(ETName))
            {
                et.Name = ETName;
                _context.Update(et);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageEventType));
            }
            else
            {
                return NoContent();
            }
        }

    }
    }