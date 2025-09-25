using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class EventModificationTests : TestBase
    {
        private async Task CreateEventWithReservations()
        {
            // Create venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Modification Test Venue");
            await Page.FillAsync("input[name='Address']", "555 Change Ave");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create SubArea
            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "Main Area");
            await Page.FillAsync("input[name='Width']", "100");
            await Page.FillAsync("input[name='Height']", "80");
            await Page.ClickAsync("button[type='submit']");

            // Create seats
            await Page.ClickAsync("a:has-text('Add Seats')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "5");
                await Page.FillAsync("input[name='Columns']", "10");
                await Page.ClickAsync("button[type='submit']");
            }

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Original Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:00");
            await Page.FillAsync("input[name='EndTime']", "21:00");
            await Page.ClickAsync("button[type='submit']");
        }

        [Test]
        public async Task ModifyEvent_WithExistingReservations_NotifiesCustomers()
        {
            // Arrange - Create event as manager
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Modify", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateEventWithReservations();

            // Customer makes reservation
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Customer", "One");
            await LoginAsync(customerEmail, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Reserve')");

            var seats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked)");
            for (int i = 0; i < Math.Min(3, seats.Count); i++)
            {
                await seats[i].ClickAsync();
            }
            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success");

            // Manager modifies event
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(managerEmail, "Test123!");

            // Act - Modify event details
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Edit')");

            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "22:30");

            // Check for notification option
            var notifyCheckbox = await Page.QuerySelectorAsync("input[name='NotifyReservations']");
            if (notifyCheckbox != null)
            {
                await Page.CheckAsync("input[name='NotifyReservations']");
            }

            await Page.ClickAsync("button[type='submit']:has-text('Save'), button[type='submit']:has-text('Update')");

            // Assert - Check for warning about existing reservations
            var warningMessage = await Page.QuerySelectorAsync("text=reservations will be notified, text=customers will be informed");
            AssertHelper.IsNotNull(warningMessage, "Should warn about notifying customers");

            // Customer checks their reservation
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(customerEmail, "Test123!");
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");

            // Look for modification indicator
            var modifiedIndicator = await Page.QuerySelectorAsync("text=Modified, text=Updated, .badge-warning");
            AssertHelper.IsNotNull(modifiedIndicator, "Customer should see event modification indicator");
        }

        [Test]
        public async Task CancelEvent_WithReservations_RefundsCustomers()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Cancel", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateEventWithReservations();

            // Multiple customers make reservations
            var customerEmails = new string[3];
            for (int i = 0; i < 3; i++)
            {
                await Page.ClickAsync("a:has-text('Logout')");
                customerEmails[i] = GenerateTestEmail();
                await RegisterUserAsync(customerEmails[i], "Test123!", $"Customer", $"Num{i + 1}");
                await LoginAsync(customerEmails[i], "Test123!");

                await Page.GotoAsync($"{BaseUrl}/Events");
                await Page.ClickAsync("text=Original Event");
                await Page.ClickAsync("a:has-text('Reserve')");

                var seat = await Page.QuerySelectorAsync(".seat.available:first-of-type, input[type='checkbox']:not(:checked):first-of-type");
                if (seat != null) await seat.ClickAsync();
                await Page.ClickAsync("button:has-text('Reserve')");
                await Page.WaitForSelectorAsync("text=success");
            }

            // Manager cancels event
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(managerEmail, "Test123!");

            // Act - Cancel the event
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Cancel Event'), button:has-text('Cancel Event')");

            // Confirm cancellation
            var confirmDialog = await Page.QuerySelectorAsync("text=Are you sure, text=This action cannot be undone");
            if (confirmDialog != null)
            {
                var reservationCount = await Page.QuerySelectorAsync("text=3 reservations, text=3 customers");
                AssertHelper.IsNotNull(reservationCount, "Should show count of affected reservations");

                await Page.ClickAsync("button:has-text('Confirm'), button:has-text('Yes, Cancel Event')");
            }

            // Assert - Event should be cancelled
            await Page.WaitForSelectorAsync("text=cancelled, text=Event has been cancelled");

            // Check customer's reservation status
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(customerEmails[0], "Test123!");
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");

            var cancelledReservation = await Page.QuerySelectorAsync("text=Cancelled, .badge-danger");
            AssertHelper.IsNotNull(cancelledReservation, "Customer's reservation should show as cancelled");

            // Check for refund status
            var refundStatus = await Page.QuerySelectorAsync("text=Refunded, text=Refund processed");
            AssertHelper.IsNotNull(refundStatus, "Should indicate refund status");
        }

        [Test]
        public async Task RescheduleEvent_OffersRebooking()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Reschedule", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateEventWithReservations();

            // Customer makes reservation
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Rebook", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Reserve')");

            var seats = await Page.QuerySelectorAllAsync(".seat.available");
            if (seats.Count > 0) await seats[0].ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success");

            // Manager reschedules event
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(managerEmail, "Test123!");

            // Act - Reschedule event
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Reschedule'), button:has-text('Reschedule')");

            // Set new date and time
            var newDate = DateTime.Now.AddDays(20);
            await Page.FillAsync("input[name='NewDate']", newDate.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='NewStartTime']", "18:00");
            await Page.FillAsync("input[name='NewEndTime']", "20:00");

            // Offer rebooking option
            var offerRebookingCheckbox = await Page.QuerySelectorAsync("input[name='OfferRebooking']");
            if (offerRebookingCheckbox != null)
            {
                await Page.CheckAsync("input[name='OfferRebooking']");
            }

            await Page.ClickAsync("button[type='submit']:has-text('Reschedule')");

            // Customer checks rebooking option
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(customerEmail, "Test123!");

            // Assert - Check for rebooking offer
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");

            var rebookingOffer = await Page.QuerySelectorAsync("text=Event rescheduled, text=New date available");
            AssertHelper.IsNotNull(rebookingOffer, "Customer should see rescheduling notification");

            // Accept rebooking
            var acceptButton = await Page.QuerySelectorAsync("button:has-text('Accept New Date'), button:has-text('Keep Reservation')");
            if (acceptButton != null)
            {
                await acceptButton.ClickAsync();
                await Page.WaitForSelectorAsync("text=confirmed for new date");
            }
        }

        [Test]
        public async Task ChangeVenue_TransfersReservations()
        {
            // Arrange - Create two venues
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Transfer", "Manager");
            await LoginAsync(managerEmail, "Test123!");

            // Create first venue
            await CreateEventWithReservations();

            // Create second venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "New Venue");
            await Page.FillAsync("input[name='Address']", "999 New Place");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "New Hall");
            await Page.FillAsync("input[name='Width']", "150");
            await Page.FillAsync("input[name='Height']", "120");
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Add Seats')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "10");
                await Page.FillAsync("input[name='Columns']", "15");
                await Page.ClickAsync("button[type='submit']");
            }

            // Customer makes reservation
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Transfer", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Reserve')");

            var seats = await Page.QuerySelectorAllAsync(".seat.available");
            for (int i = 0; i < Math.Min(2, seats.Count); i++)
            {
                await seats[i].ClickAsync();
            }
            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success");

            // Manager changes venue
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(managerEmail, "Test123!");

            // Act - Change event venue
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Edit')");

            var venueSelect = await Page.QuerySelectorAsync("select[name='VenueId']");
            if (venueSelect != null)
            {
                await Page.SelectOptionAsync("select[name='VenueId']", "New Venue");

                // Handle seat reassignment
                var reassignSeatsButton = await Page.QuerySelectorAsync("button:has-text('Reassign Seats')");
                if (reassignSeatsButton != null)
                {
                    await reassignSeatsButton.ClickAsync();
                }
            }

            await Page.ClickAsync("button[type='submit']:has-text('Save')");

            // Assert - Customer's reservation should be updated
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(customerEmail, "Test123!");
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");

            var venueChangeNotice = await Page.QuerySelectorAsync("text=Venue changed, text=New location");
            AssertHelper.IsNotNull(venueChangeNotice, "Customer should be notified of venue change");

            // Check new venue details
            await Page.ClickAsync("text=Original Event");
            var newVenueInfo = await Page.QuerySelectorAsync("text=New Venue");
            AssertHelper.IsNotNull(newVenueInfo, "Should show new venue information");
        }

        [Test]
        public async Task ModifyEventCapacity_HandlesOverbooking()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Capacity", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateEventWithReservations();

            // Multiple customers make reservations
            await Page.ClickAsync("a:has-text('Logout')");
            for (int i = 0; i < 3; i++)
            {
                var email = GenerateTestEmail();
                await RegisterUserAsync(email, "Test123!", $"Cap{i}", "User");
                await LoginAsync(email, "Test123!");

                await Page.GotoAsync($"{BaseUrl}/Events");
                await Page.ClickAsync("text=Original Event");
                await Page.ClickAsync("a:has-text('Reserve')");

                var seats = await Page.QuerySelectorAllAsync(".seat.available");
                for (int j = 0; j < Math.Min(5, seats.Count); j++)
                {
                    await seats[j].ClickAsync();
                }
                await Page.ClickAsync("button:has-text('Reserve')");
                await Page.WaitForSelectorAsync("text=success");

                await Page.ClickAsync("a:has-text('Logout')");
            }

            // Manager reduces capacity
            await LoginAsync(managerEmail, "Test123!");

            // Act - Reduce event capacity
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Edit')");

            var capacityField = await Page.QuerySelectorAsync("input[name='MaxCapacity'], input[name='Capacity']");
            if (capacityField != null)
            {
                await Page.FillAsync("input[name='MaxCapacity'], input[name='Capacity']", "10");
            }

            await Page.ClickAsync("button[type='submit']:has-text('Save')");

            // Assert - Should show overbooking warning
            var overbookingWarning = await Page.QuerySelectorAsync("text=overbooked, text=exceeds capacity");
            AssertHelper.IsNotNull(overbookingWarning, "Should warn about overbooking situation");

            // Check for resolution options
            var resolutionOptions = await Page.QuerySelectorAsync("button:has-text('Manage Overbooking'), a:has-text('Resolve')");
            AssertHelper.IsNotNull(resolutionOptions, "Should provide options to resolve overbooking");
        }

        [Test]
        public async Task AddShowTime_DuplicatesEventWithReservations()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "ShowTime", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateEventWithReservations();

            // Customer makes reservation for original show
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Show", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Reserve')");

            var seat = await Page.QuerySelectorAsync(".seat.available:first-of-type");
            if (seat != null) await seat.ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success");

            // Manager adds another show time
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(managerEmail, "Test123!");

            // Act - Add additional show time
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Add Show Time'), button:has-text('Duplicate Event')");

            // Set new time for same day
            await Page.FillAsync("input[name='NewStartTime']", "22:00");
            await Page.FillAsync("input[name='NewEndTime']", "00:00");

            await Page.ClickAsync("button[type='submit']:has-text('Add'), button[type='submit']:has-text('Create')");

            // Assert - New show time should be created
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");

            var eventElements = await Page.QuerySelectorAllAsync("text=Original Event");
            AssertHelper.GreaterOrEqual(eventElements.Count, 2, "Should have multiple show times");

            // Customer can see both show times
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(customerEmail, "Test123!");
            await Page.GotoAsync($"{BaseUrl}/Events");

            var showTimes = await Page.QuerySelectorAllAsync("text=Original Event");
            AssertHelper.GreaterOrEqual(showTimes.Count, 2, "Customer should see multiple show times");
        }

        [Test]
        public async Task MergeEvents_CombinesReservations()
        {
            // Arrange - Create two similar events
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Merge", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateEventWithReservations();

            // Create second event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Second Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "21:00");
            await Page.FillAsync("input[name='EndTime']", "23:00");
            await Page.ClickAsync("button[type='submit']");

            // Customers make reservations for both events
            await Page.ClickAsync("a:has-text('Logout')");
            var customer1Email = GenerateTestEmail();
            await RegisterUserAsync(customer1Email, "Test123!", "First", "Merger");
            await LoginAsync(customer1Email, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Original Event");
            await Page.ClickAsync("a:has-text('Reserve')");
            var seat1 = await Page.QuerySelectorAsync(".seat.available:first-of-type");
            if (seat1 != null) await seat1.ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve')");

            await Page.ClickAsync("a:has-text('Logout')");
            var customer2Email = GenerateTestEmail();
            await RegisterUserAsync(customer2Email, "Test123!", "Second", "Merger");
            await LoginAsync(customer2Email, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Second Event");
            await Page.ClickAsync("a:has-text('Reserve')");
            var seat2 = await Page.QuerySelectorAsync(".seat.available:first-of-type");
            if (seat2 != null) await seat2.ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve')");

            // Manager merges events
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(managerEmail, "Test123!");

            // Act - Merge events
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var mergeButton = await Page.QuerySelectorAsync("button:has-text('Merge Events'), a:has-text('Merge')");
            if (mergeButton != null)
            {
                await mergeButton.ClickAsync();

                // Select events to merge
                var event1Checkbox = await Page.QuerySelectorAsync("input[type='checkbox'][value*='Original']");
                var event2Checkbox = await Page.QuerySelectorAsync("input[type='checkbox'][value*='Second']");

                if (event1Checkbox != null && event2Checkbox != null)
                {
                    await event1Checkbox.CheckAsync();
                    await event2Checkbox.CheckAsync();

                    await Page.ClickAsync("button:has-text('Merge Selected')");

                    // Configure merged event
                    await Page.FillAsync("input[name='MergedEventName']", "Combined Event");
                    await Page.ClickAsync("button:has-text('Confirm Merge')");
                }
            }

            // Assert - Check merged event has all reservations
            await Page.GotoAsync($"{BaseUrl}/Events/VenueEvents");
            var mergedEvent = await Page.QuerySelectorAsync("text=Combined Event, text=Original Event");
            AssertHelper.IsNotNull(mergedEvent, "Merged event should exist");

            // Both customers should have reservations for merged event
            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(customer1Email, "Test123!");
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");
            var customer1Reservation = await Page.QuerySelectorAsync("text=Combined Event, text=Original Event");
            AssertHelper.IsNotNull(customer1Reservation, "First customer's reservation should be transferred");

            await Page.ClickAsync("a:has-text('Logout')");
            await LoginAsync(customer2Email, "Test123!");
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");
            var customer2Reservation = await Page.QuerySelectorAsync("text=Combined Event, text=Original Event");
            AssertHelper.IsNotNull(customer2Reservation, "Second customer's reservation should be transferred");
        }
    }
}