using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Data;
using System.Threading.Tasks;

namespace PtixiakiReservations.Controllers;

public class AdminController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var dashboardStats = new
        {
            VenueCount = await context.Venue.CountAsync(),
            EventCount = await context.Event.CountAsync(),
            SubAreaCount = await context.SubArea.CountAsync(),
            ReservationCount = await context.Reservation.CountAsync()
        };

        return View(dashboardStats);
    }
}