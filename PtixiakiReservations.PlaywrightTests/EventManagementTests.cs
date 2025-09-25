using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class EventManagementTests : TestBase
    {
        private async Task CreateTestVenue(string venueName = "Test Venue")
        {
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", venueName);
            await Page.FillAsync("input[name='Address']", "123 Test St");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForURLAsync(url => !url.Contains("/Create"));
        }

        [Test]
        public async Task CreateEvent_SingleDay_Success()
        {
            // Arrange - Create venue manager and venue
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Event", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateTestVenue("Single Day Venue");

            // Navigate to event creation
            await Page.ClickAsync("a:has-text('Create Event'), button:has-text('Create Event')");

            // Act - Fill event form
            await Page.FillAsync("input[name='Name']", "Summer Concert");
            await Page.FillAsync("textarea[name='Description']", "Amazing summer concert event");

            // Set date and time
            var eventDate = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
            await Page.FillAsync("input[name='StartDate']", eventDate);
            await Page.FillAsync("input[name='StartTime']", "19:00");
            await Page.FillAsync("input[name='EndTime']", "22:00");

            // Set price if field exists
            var priceField = await Page.QuerySelectorAsync("input[name='Price']");
            if (priceField != null)
            {
                await Page.FillAsync("input[name='Price']", "50.00");
            }

            // Select category if dropdown exists
            var categorySelect = await Page.QuerySelectorAsync("select[name='Category']");
            if (categorySelect != null)
            {
                await Page.SelectOptionAsync("select[name='Category']", new SelectOptionValue { Index = 1 });
            }

            // Submit form
            await Page.ClickAsync("button[type='submit']:has-text('Create')");

            // Assert - Verify event was created
            await Page.WaitForSelectorAsync("text=Summer Concert");
            var eventElement = await Page.QuerySelectorAsync("h1:has-text('Summer Concert'), h2:has-text('Summer Concert')");
            AssertHelper.IsNotNull(eventElement, "Event should be created and displayed");
        }

        [Test]
        public async Task CreateEvent_MultipleDays_CreatesIndividualEvents()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Multi", "Event");
            await LoginAsync(email, "Test123!");
            await CreateTestVenue("Multi Day Venue");

            // Navigate to event creation
            await Page.ClickAsync("a:has-text('Create Event'), button:has-text('Create Event')");

            // Act - Fill multi-day event form
            await Page.FillAsync("input[name='Name']", "3-Day Festival");
            await Page.FillAsync("textarea[name='Description']", "Music festival spanning 3 days");

            // Set date range
            var startDate = DateTime.Now.AddDays(60);
            await Page.FillAsync("input[name='StartDate']", startDate.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='EndDate']", startDate.AddDays(2).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "18:00");
            await Page.FillAsync("input[name='EndTime']", "23:00");

            // Check "Create events for multiple days" if checkbox exists
            var multiDayCheckbox = await Page.QuerySelectorAsync("input[name='CreateMultipleDayEvents']");
            if (multiDayCheckbox != null)
            {
                await Page.CheckAsync("input[name='CreateMultipleDayEvents']");
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert - Verify multiple events were created
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");

            // Should see 3 separate events
            var eventElements = await Page.QuerySelectorAllAsync("text=3-Day Festival");
            AssertHelper.GreaterOrEqual(eventElements.Count, 3, "Should create 3 individual events for 3 days");
        }

        [Test]
        public async Task EditEvent_UpdatesDetails()
        {
            // Arrange - Create event
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Edit", "Event");
            await LoginAsync(email, "Test123!");
            await CreateTestVenue("Edit Event Venue");

            // Create initial event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Original Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(15).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "22:00");
            await Page.ClickAsync("button[type='submit']");

            // Act - Edit the event
            await Page.ClickAsync("a:has-text('Edit')");

            await Page.FillAsync("input[name='Name']", "Updated Event Name");
            await Page.FillAsync("textarea[name='Description']", "Updated description with more details");
            await Page.FillAsync("input[name='StartTime']", "19:30");
            await Page.FillAsync("input[name='EndTime']", "23:00");

            await Page.ClickAsync("button[type='submit']:has-text('Save'), button[type='submit']:has-text('Update')");

            // Assert - Verify updates
            await Page.WaitForSelectorAsync("text=Updated Event Name");
            var updatedName = await Page.TextContentAsync("h1, h2, .event-name");
            Assert.That(updatedName, Does.Contain("Updated Event Name"));
        }

        [Test]
        public async Task ViewVenueEvents_DisplaysAllEvents()
        {
            // Arrange - Create venue with multiple events
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "View", "Events");
            await LoginAsync(email, "Test123!");
            await CreateTestVenue("Event List Venue");

            // Create multiple events
            string[] eventNames = { "Morning Workshop", "Afternoon Concert", "Evening Gala" };

            foreach (var eventName in eventNames)
            {
                await Page.ClickAsync("a:has-text('Create Event')");
                await Page.FillAsync("input[name='Name']", eventName);
                await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(20).ToString("yyyy-MM-dd"));
                await Page.FillAsync("input[name='StartTime']", "19:00");
                await Page.FillAsync("input[name='EndTime']", "21:00");
                await Page.ClickAsync("button[type='submit']");
                await Page.WaitForTimeoutAsync(500);
            }

            // Act - View venue events
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");

            // Assert - All events should be visible
            foreach (var eventName in eventNames)
            {
                var eventElement = await Page.QuerySelectorAsync($"text={eventName}");
                AssertHelper.IsNotNull(eventElement, $"Event '{eventName}' should be displayed");
            }
        }

        [Test]
        public async Task CreateRecurringEvents_Weekly()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Recurring", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateTestVenue("Recurring Events Venue");

            // Navigate to event generator or creation page
            await Page.ClickAsync("a:has-text('Event Generator'), a:has-text('Create Event')");

            // Act - Set up recurring event
            await Page.FillAsync("input[name='Name']", "Weekly Yoga Class");
            await Page.FillAsync("textarea[name='Description']", "Relaxing yoga sessions every week");

            // Set start date to next Monday
            var nextMonday = DateTime.Now.AddDays((8 - (int)DateTime.Now.DayOfWeek) % 7);
            if (nextMonday <= DateTime.Now) nextMonday = nextMonday.AddDays(7);

            await Page.FillAsync("input[name='StartDate']", nextMonday.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "09:00");
            await Page.FillAsync("input[name='EndTime']", "10:30");

            // Set recurrence pattern if available
            var recurrenceSelect = await Page.QuerySelectorAsync("select[name='RecurrencePattern']");
            if (recurrenceSelect != null)
            {
                await Page.SelectOptionAsync("select[name='RecurrencePattern']", "Weekly");
                await Page.FillAsync("input[name='RecurrenceCount']", "4");
            }

            await Page.ClickAsync("button[type='submit']");

            // Assert - Verify recurring events created
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");

            var yogaEvents = await Page.QuerySelectorAllAsync("text=Weekly Yoga Class");
            AssertHelper.GreaterOrEqual(yogaEvents.Count, 4, "Should create 4 weekly recurring events");
        }

        [Test]
        public async Task DeleteEvent_RemovesFromList()
        {
            // Arrange - Create event to delete
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Delete", "Event");
            await LoginAsync(email, "Test123!");
            await CreateTestVenue("Delete Test Venue");

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Event to Delete");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "15:00");
            await Page.FillAsync("input[name='EndTime']", "17:00");
            await Page.ClickAsync("button[type='submit']");

            // Act - Delete the event
            await Page.ClickAsync("a:has-text('Delete'), button:has-text('Delete')");

            // Confirm deletion if needed
            var confirmButton = await Page.QuerySelectorAsync("button:has-text('Confirm'), button:has-text('Yes')");
            if (confirmButton != null)
            {
                await confirmButton.ClickAsync();
            }

            // Assert - Event should be removed
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var deletedEvent = await Page.QuerySelectorAsync("text=Event to Delete");
            AssertHelper.IsNull(deletedEvent, "Deleted event should not be displayed");
        }
    }
}