using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PtixiakiReservations.Services;

namespace PtixiakiReservations.Controllers;

[Authorize]
public class EventGeneratorController : Controller
{
    private readonly IEventGeneratorService _eventGeneratorService;
    private readonly ILogger<EventGeneratorController> _logger;

    public EventGeneratorController(
        IEventGeneratorService eventGeneratorService,
        ILogger<EventGeneratorController> logger)
    {
        _eventGeneratorService = eventGeneratorService;
        _logger = logger;
    }

    // GET: EventGenerator
    public IActionResult Index()
    {
        return View(new EventGenerationOptions());
    }

    // POST: EventGenerator/Generate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(EventGenerationOptions options)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", options);
        }

        try
        {
            _logger.LogInformation("Starting event generation with {VenueCount} venues", options.VenueCount);
            
            var result = await _eventGeneratorService.GenerateEventsAsync(options);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Successfully generated {result.TotalItemsGenerated} items: " +
                    $"{result.GeneratedVenues.Count} venues, " +
                    $"{result.GeneratedSubAreas.Count} sub-areas, " +
                    $"{result.GeneratedSeats.Count} seats, " +
                    $"{result.GeneratedEvents.Count} events";
                    
                _logger.LogInformation("Event generation completed successfully. Generated {TotalItems} items", result.TotalItemsGenerated);
                
                return View("Success", result);
            }
            else
            {
                TempData["ErrorMessage"] = $"Event generation failed: {result.ErrorMessage}";
                _logger.LogError("Event generation failed: {ErrorMessage}", result.ErrorMessage);
                return View("Index", options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during event generation");
            TempData["ErrorMessage"] = $"An error occurred during generation: {ex.Message}";
            return View("Index", options);
        }
    }

    // POST: EventGenerator/Cleanup
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cleanup()
    {
        try
        {
            _logger.LogInformation("Starting cleanup of generated data");
            
            var success = await _eventGeneratorService.CleanupGeneratedDataAsync();
            
            if (success)
            {
                TempData["SuccessMessage"] = "Successfully cleaned up all generated data";
                _logger.LogInformation("Cleanup completed successfully");
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cleanup generated data";
                _logger.LogWarning("Cleanup operation failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
            TempData["ErrorMessage"] = $"An error occurred during cleanup: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    // GET: EventGenerator/Preview
    public IActionResult Preview(EventGenerationOptions options)
    {
        // Calculate estimated generation counts for preview
        var preview = new EventGenerationPreview
        {
            EstimatedVenues = options.VenueCount,
            EstimatedSubAreas = options.VenueCount * ((options.MinSubAreasPerVenue + options.MaxSubAreasPerVenue) / 2),
            EstimatedEvents = options.VenueCount * ((options.MinEventsPerVenue + options.MaxEventsPerVenue) / 2),
            EstimatedSeats = options.GenerateSeats ? 
                options.VenueCount * ((options.MinSubAreasPerVenue + options.MaxSubAreasPerVenue) / 2) * ((options.MinSeatsPerSubArea + options.MaxSeatsPerSubArea) / 2) : 0,
            DateRange = $"{DateTime.Now.AddDays(options.MinDaysInFuture):yyyy-MM-dd} to {DateTime.Now.AddDays(options.MaxDaysInFuture):yyyy-MM-dd}"
        };

        return Json(preview);
    }
}

public class EventGenerationPreview
{
    public int EstimatedVenues { get; set; }
    public int EstimatedSubAreas { get; set; }
    public int EstimatedEvents { get; set; }
    public int EstimatedSeats { get; set; }
    public string DateRange { get; set; } = string.Empty;
    public int TotalEstimatedItems => EstimatedVenues + EstimatedSubAreas + EstimatedEvents + EstimatedSeats;
}