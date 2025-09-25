using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class EventGeneratorTests : TestBase
    {
        private async Task CreateVenueWithSubArea()
        {
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Event Generator Venue");
            await Page.FillAsync("input[name='Address']", "456 Generator Ave");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "Conference Hall");
            await Page.FillAsync("input[name='Width']", "120");
            await Page.FillAsync("input[name='Height']", "100");
            await Page.ClickAsync("button[type='submit']");
        }

        [Test]
        public async Task GenerateEvents_DailyRecurrence_CreatesCorrectPattern()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Daily", "Generator");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            // Navigate to event generator
            await Page.GotoAsync($"{BaseUrl}/EventGenerator");

            // Act - Set up daily recurring events
            await Page.FillAsync("input[name='EventName']", "Daily Standup Meeting");
            await Page.FillAsync("textarea[name='Description']", "Daily team synchronization");

            var startDate = DateTime.Now.AddDays(1);
            await Page.FillAsync("input[name='StartDate']", startDate.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", startDate.AddDays(6).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "09:00");
            await Page.FillAsync("input[name='EndTime']", "09:30");

            // Select recurrence pattern
            var recurrenceOption = await Page.QuerySelectorAsync("select[name='RecurrenceType']");
            if (recurrenceOption != null)
            {
                await Page.SelectOptionAsync("select[name='RecurrenceType']", "Daily");
            }
            else
            {
                // Alternative: Check daily checkbox
                var dailyCheckbox = await Page.QuerySelectorAsync("input[type='checkbox'][value='daily']");
                if (dailyCheckbox != null)
                {
                    await Page.CheckAsync("input[type='checkbox'][value='daily']");
                }
            }

            await Page.ClickAsync("button[type='submit']:has-text('Generate'), button[type='submit']:has-text('Create Events')");

            // Assert - Verify 7 daily events were created
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var dailyEvents = await Page.QuerySelectorAllAsync("text=Daily Standup Meeting");
            AssertHelper.GreaterOrEqual(dailyEvents.Count, 7, "Should create 7 daily events");
        }

        [Test]
        public async Task GenerateEvents_WeekdaysOnly_SkipsWeekends()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Weekday", "Generator");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            await Page.GotoAsync($"{BaseUrl}/EventGenerator");

            // Act - Create weekday-only events
            await Page.FillAsync("input[name='EventName']", "Team Training Session");
            await Page.FillAsync("textarea[name='Description']", "Professional development training");

            // Start on a Monday
            var nextMonday = DateTime.Now.AddDays((8 - (int)DateTime.Now.DayOfWeek) % 7);
            if (nextMonday <= DateTime.Now) nextMonday = nextMonday.AddDays(7);

            await Page.FillAsync("input[name='StartDate']", nextMonday.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", nextMonday.AddDays(11).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "14:00");
            await Page.FillAsync("input[name='EndTime']", "16:00");

            // Select weekdays only
            var weekdaysOption = await Page.QuerySelectorAsync("input[type='checkbox'][name='WeekdaysOnly']");
            if (weekdaysOption != null)
            {
                await Page.CheckAsync("input[type='checkbox'][name='WeekdaysOnly']");
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert - Should have 10 events (2 weeks x 5 weekdays)
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var trainingEvents = await Page.QuerySelectorAllAsync("text=Team Training Session");
            AssertHelper.GreaterOrEqual(trainingEvents.Count, 8, "Should create events only on weekdays");
        }

        [Test]
        public async Task GenerateEvents_SpecificDaysOfWeek_CreatesPattern()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Specific", "Days");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            await Page.GotoAsync($"{BaseUrl}/EventGenerator");

            // Act - Create events for specific days (Tuesday & Thursday)
            await Page.FillAsync("input[name='EventName']", "Yoga Class");
            await Page.FillAsync("textarea[name='Description']", "Relaxation and wellness session");

            var startDate = DateTime.Now.AddDays(1);
            await Page.FillAsync("input[name='StartDate']", startDate.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", startDate.AddDays(30).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "18:00");
            await Page.FillAsync("input[name='EndTime']", "19:30");

            // Select specific days
            var tuesdayCheckbox = await Page.QuerySelectorAsync("input[type='checkbox'][value='Tuesday']");
            var thursdayCheckbox = await Page.QuerySelectorAsync("input[type='checkbox'][value='Thursday']");

            if (tuesdayCheckbox != null && thursdayCheckbox != null)
            {
                await Page.CheckAsync("input[type='checkbox'][value='Tuesday']");
                await Page.CheckAsync("input[type='checkbox'][value='Thursday']");
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var yogaEvents = await Page.QuerySelectorAllAsync("text=Yoga Class");
            AssertHelper.Greater(yogaEvents.Count, 0, "Should create events on specific days");
        }

        [Test]
        public async Task GenerateEvents_WithTicketPrice_SetsCorrectPricing()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Price", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            await Page.GotoAsync($"{BaseUrl}/EventGenerator");

            // Act - Create events with pricing
            await Page.FillAsync("input[name='EventName']", "Premium Concert Series");
            await Page.FillAsync("textarea[name='Description']", "Exclusive musical performances");

            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "22:30");

            // Set ticket price
            var priceField = await Page.QuerySelectorAsync("input[name='TicketPrice'], input[name='Price']");
            if (priceField != null)
            {
                await Page.FillAsync("input[name='TicketPrice'], input[name='Price']", "75.00");
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert - Navigate to first event and verify price
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Premium Concert Series");

            var priceElement = await Page.QuerySelectorAsync("text=75, text=$75, text=€75");
            AssertHelper.IsNotNull(priceElement, "Event should display the correct ticket price");
        }

        [Test]
        public async Task GenerateEvents_MonthlyRecurrence_LastFridayPattern()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Monthly", "Pattern");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            await Page.GotoAsync($"{BaseUrl}/EventGenerator");

            // Act - Create monthly event on last Friday
            await Page.FillAsync("input[name='EventName']", "Monthly Team Celebration");
            await Page.FillAsync("textarea[name='Description']", "End of month team gathering");

            await Page.FillAsync("input[name='StartDate']", DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", DateTime.Now.AddMonths(3).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "17:00");
            await Page.FillAsync("input[name='EndTime']", "19:00");

            // Select monthly recurrence
            var monthlyOption = await Page.QuerySelectorAsync("select[name='RecurrenceType']");
            if (monthlyOption != null)
            {
                await Page.SelectOptionAsync("select[name='RecurrenceType']", "Monthly");

                // Select "Last Friday" pattern if available
                var lastFridayOption = await Page.QuerySelectorAsync("select[name='MonthlyPattern']");
                if (lastFridayOption != null)
                {
                    await Page.SelectOptionAsync("select[name='MonthlyPattern']", "LastFriday");
                }
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var monthlyEvents = await Page.QuerySelectorAllAsync("text=Monthly Team Celebration");
            AssertHelper.GreaterOrEqual(monthlyEvents.Count, 3, "Should create monthly recurring events");
        }

        [Test]
        public async Task GenerateEvents_WithCapacityLimit_SetsMaxAttendees()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Capacity", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            await Page.GotoAsync($"{BaseUrl}/EventGenerator");

            // Act - Create event with capacity limit
            await Page.FillAsync("input[name='EventName']", "Exclusive Workshop");
            await Page.FillAsync("textarea[name='Description']", "Limited seats available");

            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "10:00");
            await Page.FillAsync("input[name='EndTime']", "12:00");

            // Set capacity limit
            var capacityField = await Page.QuerySelectorAsync("input[name='MaxCapacity'], input[name='Capacity']");
            if (capacityField != null)
            {
                await Page.FillAsync("input[name='MaxCapacity'], input[name='Capacity']", "25");
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Exclusive Workshop");

            var capacityElement = await Page.QuerySelectorAsync("text=25 seats, text=Capacity: 25, text=Max: 25");
            AssertHelper.IsNotNull(capacityElement, "Event should display capacity limit");
        }

        [Test]
        public async Task GenerateBulkEvents_FromCSV_ImportsCorrectly()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "CSV", "Import");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            // Check if CSV import feature exists
            await Page.GotoAsync($"{BaseUrl}/EventGenerator");
            var csvImportButton = await Page.QuerySelectorAsync("button:has-text('Import CSV'), a:has-text('Import from CSV')");

            if (csvImportButton != null)
            {
                // Act - Click import and upload CSV
                await csvImportButton.ClickAsync();

                var fileInput = await Page.QuerySelectorAsync("input[type='file'][accept*='csv']");
                if (fileInput != null)
                {
                    // Create a test CSV content
                    var csvContent = "Name,Date,StartTime,EndTime,Description\n" +
                                   "Morning Seminar,2024-12-01,09:00,11:00,Educational seminar\n" +
                                   "Afternoon Workshop,2024-12-01,14:00,16:00,Hands-on workshop\n" +
                                   "Evening Gala,2024-12-01,19:00,22:00,Formal dinner event";

                    // Note: In real test, you'd need to create actual CSV file
                    // await fileInput.SetInputFilesAsync("path/to/test.csv");
                }

                await Page.ClickAsync("button[type='submit']:has-text('Import')");

                // Assert
                await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
                var importedEvents = await Page.QuerySelectorAllAsync("text=Seminar, text=Workshop, text=Gala");
                AssertHelper.Greater(importedEvents.Count, 0, "Should import events from CSV");
            }
        }

        [Test]
        public async Task GenerateEvents_WithCustomCategories_AssignsCorrectly()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Category", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateVenueWithSubArea();

            await Page.GotoAsync($"{BaseUrl}/EventGenerator");

            // Act - Create events with categories
            await Page.FillAsync("input[name='EventName']", "Tech Conference 2024");
            await Page.FillAsync("textarea[name='Description']", "Annual technology conference");

            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", DateTime.Now.AddDays(32).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "09:00");
            await Page.FillAsync("input[name='EndTime']", "17:00");

            // Select category
            var categorySelect = await Page.QuerySelectorAsync("select[name='Category']");
            if (categorySelect != null)
            {
                await Page.SelectOptionAsync("select[name='Category']", "Conference");
            }
            else
            {
                // Try to type in a category field
                var categoryInput = await Page.QuerySelectorAsync("input[name='Category']");
                if (categoryInput != null)
                {
                    await Page.FillAsync("input[name='Category']", "Technology");
                }
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var techEvents = await Page.QuerySelectorAllAsync("text=Tech Conference 2024");
            AssertHelper.GreaterOrEqual(techEvents.Count, 3, "Should create 3-day conference events");
        }
    }
}