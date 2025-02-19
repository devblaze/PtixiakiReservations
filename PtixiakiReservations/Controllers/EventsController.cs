using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Models.ViewModels;

namespace PtixiakiReservations.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        public EventsController(ApplicationDbContext context, UserManager<ApplicationUser> _userManager, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            userManager = _userManager;
            this.roleManager = roleManager;
        }

        // GET: Events
        public IActionResult Index()
        {

            return View();
        }

        public async Task<IActionResult> EventsForToday(String City)
        {
            var applicationDbContext = _context.Event
                .Include(r => r.FamilyEvent)
                .Include(r => r.Venue)
                .Where(e => e.StartDateTime.Date == DateTime.Now.Date && e.EndTime.TimeOfDay > DateTime.Now.TimeOfDay);
            if (City == null)
            {
                return View(await applicationDbContext.ToListAsync());
            }

            applicationDbContext = applicationDbContext.Where(r => r.Venue.City.Name == City);

            return View(await applicationDbContext.ToListAsync());
        }
        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Event
                .Include(r => r.FamilyEvent)
                .Include(r => r.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: Events/Create      
        public JsonResult GetEvents()
        {
            var events = _context.Event.Include(e => e.EventType)
            .Where(e => e.Venue.ApplicationUser.Id == userManager.GetUserId(HttpContext.User)).ToList();
            return new JsonResult(events);
        }
        public JsonResult GetEvents2(int? venueId)
        {
            var events = _context.Event.Where(e => e.Venue.Id == venueId).ToList();

            return new JsonResult(events);
        }

        public JsonResult GetEventTypes()
        {
            var eventsTypes = _context.EventType.ToList();

            return new JsonResult(eventsTypes);
        }
        public IActionResult VenueEvents(int venueId)
        {

            var Venue = _context.Venue.SingleOrDefault(e => e.Id == venueId);

            return View(Venue);
        }


        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] JsonEventModel ev)
        {
            if (ev == null)
            {
                return NotFound();
            }
            else
            {
                var Venue = _context.Venue.SingleOrDefault(v => v.ApplicationUser.Id == userManager.GetUserId(HttpContext.User));
                var FamilyEvent = _context.FamilyEvent.SingleOrDefault(f => f.Name == ev.Name);
                int days = 0;
                if (Venue == null)
                {
                    return NotFound();
                }
                else if (FamilyEvent == null)
                {
                    FamilyEvent familyEvent = new FamilyEvent
                    {
                        Name = ev.Name
                    };
                    _context.Add(familyEvent);
                    await _context.SaveChangesAsync();
                    FamilyEvent = _context.FamilyEvent.SingleOrDefault(f => f.Name == ev.Name);
                }
                int everyNum = 0;
                switch (ev.Repeat.SelectRepeat)
                {
                    case "Day":
                        if (ev.Repeat.AfterNumTimes != "")
                        {
                            for (int i = 0; i < int.Parse(ev.Repeat.AfterNumTimes); i++)
                            {

                                Event nEvent = new Event
                                {
                                    Name = ev.Name,
                                    StartDateTime = ev.StartDateTime.AddDays(everyNum).ToLocalTime(),
                                    EndTime = ev.EndTime.AddDays(everyNum).ToLocalTime(),
                                    EventTypeId = int.Parse(ev.EventTypeId),
                                    VenueId = Venue.Id,
                                    FamilyEventId = FamilyEvent.Id

                                };
                                everyNum += int.Parse(ev.Repeat.NumOfRepeat);
                                _context.Add(nEvent);
                            }
                        }
                        else
                        {
                            while (ev.Repeat.UntilDate > ev.StartDateTime.AddDays(everyNum)) {
                                Event nEvent = new Event
                                {
                                    Name = ev.Name,
                                    StartDateTime = ev.StartDateTime.AddDays(everyNum).ToLocalTime(),
                                    EndTime = ev.EndTime.AddDays(everyNum).ToLocalTime(),
                                    EventTypeId = int.Parse(ev.EventTypeId),
                                    VenueId = Venue.Id,
                                    FamilyEventId = FamilyEvent.Id

                                };
                                everyNum += int.Parse(ev.Repeat.NumOfRepeat);
                                _context.Add(nEvent);
                            }
                        }
                        break;
                    case "Week":
                       
                        if (ev.Repeat.AfterNumTimes != "")
                        {
                            for (int i = 0; i < int.Parse(ev.Repeat.AfterNumTimes)*7; i++)
                            {                                
                                if (CorrectDay(ev,i, everyNum) == true)
                                {
                                    Event nEvent = new Event
                                    {
                                        Name = ev.Name,
                                        StartDateTime = ev.StartDateTime.AddDays(i+ (everyNum * 7)).ToLocalTime(),
                                        EndTime = ev.EndTime.AddDays(i+ (everyNum * 7)).ToLocalTime(),
                                        EventTypeId = int.Parse(ev.EventTypeId),
                                        VenueId = Venue.Id,
                                        FamilyEventId = FamilyEvent.Id

                                    };
                                   
                                     _context.Add(nEvent);
                                }
                                if (i+1 >= 7)
                                {
                                    if ((i+1) % 7 == 0)
                                    {                                     
                                            everyNum += (int.Parse(ev.Repeat.NumOfRepeat)-1);
                                        
                                    }
                                }
                            }
                        }
                        else
                        {
                            int i = 0;
                            while (ev.Repeat.UntilDate > ev.StartDateTime.AddDays(i))
                            {
                                if (CorrectDay(ev, i,everyNum))
                                {
                                    Event nEvent = new Event
                                    {
                                        Name = ev.Name,
                                        StartDateTime = ev.StartDateTime.AddDays(i+ (everyNum*7)).ToLocalTime(),
                                        EndTime = ev.EndTime.AddDays(i + (everyNum * 7)).ToLocalTime(),
                                        EventTypeId = int.Parse(ev.EventTypeId),
                                        VenueId = Venue.Id,
                                        FamilyEventId = FamilyEvent.Id

                                    };
                                    
                                    _context.Add(nEvent);
                                }
                                if (i + 1 >= 7)
                                {
                                    if ((i + 1) % 7 == 0)
                                    {
                                        everyNum += (int.Parse(ev.Repeat.NumOfRepeat) - 1);

                                    }
                                }
                                i++;
                            }
                        }
                        break;
                    case "Month":
                        if (ev.Repeat.AfterNumTimes != "")
                        {
                            for (int i = 0; i < int.Parse(ev.Repeat.AfterNumTimes); i++)
                            {

                                Event nEvent = new Event
                                {
                                    Name = ev.Name,
                                    StartDateTime = ev.StartDateTime.AddMonths(everyNum).ToLocalTime(),
                                    EndTime = ev.EndTime.AddMonths(everyNum).ToLocalTime(),
                                    EventTypeId = int.Parse(ev.EventTypeId),
                                    VenueId = Venue.Id,
                                    FamilyEventId = FamilyEvent.Id

                                };
                                everyNum += int.Parse(ev.Repeat.NumOfRepeat);
                                _context.Add(nEvent);
                            }
                        }
                        else
                        {
                            while (ev.Repeat.UntilDate > ev.StartDateTime.AddMonths(everyNum))
                            {
                                Event nEvent = new Event
                                {
                                    Name = ev.Name,
                                    StartDateTime = ev.StartDateTime.AddMonths(everyNum).ToLocalTime(),
                                    EndTime = ev.EndTime.AddMonths(everyNum).ToLocalTime(),
                                    EventTypeId = int.Parse(ev.EventTypeId),
                                    VenueId = Venue.Id,
                                    FamilyEventId = FamilyEvent.Id

                                };
                                everyNum += int.Parse(ev.Repeat.NumOfRepeat);
                                _context.Add(nEvent);
                            }
                        }
                        break;
                }            
                         
            }
            await _context.SaveChangesAsync();

            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(Response.StatusCode);
        }  

        public bool CorrectDay(JsonEventModel ev,int i,int everyNum)
        {
            bool correctDay = false;
            if (ev.Repeat.M == true && ev.StartDateTime.AddDays(i+everyNum * 7).DayOfWeek.ToString() == "Monday")
            {
                correctDay = true;
            }
            else if (ev.Repeat.Tu == true && ev.StartDateTime.AddDays(i+everyNum * 7).DayOfWeek.ToString() == "Tuesday")
            {
                correctDay = true;
            }
            else if (ev.Repeat.W == true && ev.StartDateTime.AddDays(i+everyNum * 7).DayOfWeek.ToString() == "Wednesday")
            {
                correctDay = true;
            }
            else if (ev.Repeat.Th == true && ev.StartDateTime.AddDays(i+everyNum * 7).DayOfWeek.ToString() == "Thursday")
            {
                correctDay = true;

            }
            else if (ev.Repeat.F == true && ev.StartDateTime.AddDays(i+everyNum * 7).DayOfWeek.ToString() == "Friday")
            {
                correctDay = true;
            }
            else if (ev.Repeat.Sa == true && ev.StartDateTime.AddDays(i+everyNum * 7).DayOfWeek.ToString() == "Saturday")
            {
                correctDay = true;
            }
            else if (ev.Repeat.Su == true && ev.StartDateTime.AddDays(i+everyNum * 7).DayOfWeek.ToString() == "Sunday")
            {
                correctDay = true;
            }
            return correctDay;
        }

        public async Task<IActionResult> Delete(int? id,bool dAll)
        {

            if (id == null)
            {
                return NotFound();
            }
            var ev = await _context.Event
                  .Include(r => r.FamilyEvent)
                  .Include(r => r.Venue)
                  .Include(r => r.EventType)
                  .FirstOrDefaultAsync(m => m.Id == id);
            if (ev == null)
            {
                return NotFound();
            }
            if (dAll == true)
            {
                
                var familyEvents = _context.Event.Include(r => r.FamilyEvent).Include(r => r.EventType)
                    .Where(e => e.FamilyEventId == ev.FamilyEventId).ToList();
                foreach(var @event in familyEvents)
                {
                    var hasReservations = _context.Reservation.Where(r => r.EventId == @event.Id).ToList();
                    _context.Reservation.RemoveRange(hasReservations);
                }
                     _context.Event.RemoveRange(familyEvents);                
            }                         
                else
                {
                    var hasReservations = _context.Reservation.Where(r => r.EventId == ev.Id).ToList();
                    if (hasReservations != null)
                {
                    _context.Reservation.RemoveRange(hasReservations);
                }
                    _context.Event.Remove(ev);
                
            }

            await _context.SaveChangesAsync();
            Response.StatusCode = (int)HttpStatusCode.OK;
            return Json(Response.StatusCode);
        }
      
        private bool EventExists(int id)
        {
            return _context.Event.Any(e => e.Id == id);
        }
    }
}
