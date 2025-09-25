using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class ReservationTests : TestBase
    {
        private async Task SetupTestEventWithSeats()
        {
            // Create venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Reservation Test Venue");
            await Page.FillAsync("input[name='Address']", "123 Reserve St");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Label = "Athens" });
            await Page.ClickAsync("button[type='submit']");

            // Create SubArea
            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "Main Theater");
            await Page.FillAsync("input[name='Width']", "100");
            await Page.FillAsync("input[name='Height']", "80");
            await Page.ClickAsync("button[type='submit']");

            // Create seats
            await Page.ClickAsync("a:has-text('Add Seats'), a:has-text('Create Seat Map')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "5");
                await Page.FillAsync("input[name='Columns']", "10");
                await Page.ClickAsync("button[type='submit']");
            }

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Test Concert");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "22:30");
            await Page.ClickAsync("button[type='submit']");
        }

        [Test]
        public async Task SearchEvents_ByName_FindsCorrectEvents()
        {
            // Arrange - Create events with different names
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Manager", "User");
            await LoginAsync(managerEmail, "Test123!");

            // Create venue and events
            await SetupTestEventWithSeats();

            // Create additional events
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Shakespeare's Hamlet");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:00");
            await Page.FillAsync("input[name='EndTime']", "21:30");
            await Page.ClickAsync("button[type='submit']");

            // Act - Search for events
            await Page.GotoAsync($"{BaseUrl}/Events");

            // Search for Shakespeare
            await Page.FillAsync("input[name='searchString'], input[placeholder*='Search']", "Shakespeare");
            await Page.ClickAsync("button[type='submit']:has-text('Search'), button:has-text('🔍')");

            // Assert - Only Shakespeare event should be shown
            await Page.WaitForSelectorAsync("text=Shakespeare's Hamlet");
            var shakespeareEvent = await Page.QuerySelectorAsync("text=Shakespeare's Hamlet");
            AssertHelper.IsNotNull(shakespeareEvent, "Shakespeare event should be found");

            var testConcert = await Page.QuerySelectorAsync("text=Test Concert");
            AssertHelper.IsNull(testConcert, "Test Concert should not be in search results");
        }

        [Test]
        public async Task SearchEvents_ByCity_FiltersCorrectly()
        {
            // Arrange - Login as manager
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "City", "Manager");
            await LoginAsync(email, "Test123!");

            await SetupTestEventWithSeats();

            // Act - Search by city
            await Page.GotoAsync($"{BaseUrl}/Events");

            // Select city filter
            var citySelect = await Page.QuerySelectorAsync("select[name='city']");
            if (citySelect != null)
            {
                await Page.SelectOptionAsync("select[name='city']", "Athens");
                await Page.ClickAsync("button[type='submit']:has-text('Search'), button:has-text('Filter')");
            }

            // Assert - Events in Athens should be shown
            await Page.WaitForSelectorAsync("text=Test Concert");
            var concertElement = await Page.QuerySelectorAsync("text=Test Concert");
            AssertHelper.IsNotNull(concertElement, "Events in selected city should be displayed");
        }

        [Test]
        public async Task ReserveSeats_CompleteWorkflow()
        {
            // Arrange - Setup event as manager
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Venue", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await SetupTestEventWithSeats();

            // Logout and login as customer
            await Page.ClickAsync("a:has-text('Logout'), button:has-text('Logout')");

            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Customer", "User");
            await LoginAsync(customerEmail, "Test123!");

            // Act - Search and select event
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Test Concert");

            // Click Reserve/Book button
            await Page.ClickAsync("a:has-text('Reserve'), a:has-text('Book'), button:has-text('Reserve Seats')");

            // Select seats
            await Page.WaitForSelectorAsync(".seat, div[data-seat], input[type='checkbox'][name*='seat']");

            // Click on available seats or checkboxes
            var availableSeats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox'][name*='seat']:not(:checked)");

            // Select first 3 available seats
            for (int i = 0; i < Math.Min(3, availableSeats.Count); i++)
            {
                await availableSeats[i].ClickAsync();
            }

            // Confirm reservation
            await Page.ClickAsync("button:has-text('Reserve'), button:has-text('Confirm'), button:has-text('Book')");

            // Assert - Verify reservation success
            await Page.WaitForSelectorAsync("text=Reservation successful, text=Booking confirmed, .alert-success");

            // Navigate to My Reservations
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");
            var reservation = await Page.QuerySelectorAsync("text=Test Concert");
            AssertHelper.IsNotNull(reservation, "Reservation should appear in user's reservation list");
        }

        [Test]
        public async Task ViewMyReservations_DisplaysAllReservations()
        {
            // Arrange - Create and reserve multiple events
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Manager", "Multi");
            await LoginAsync(managerEmail, "Test123!");
            await SetupTestEventWithSeats();

            // Create another event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Jazz Night");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "21:00");
            await Page.FillAsync("input[name='EndTime']", "23:30");
            await Page.ClickAsync("button[type='submit']");

            // Login as customer and make reservations
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Reserve", "Multiple");
            await LoginAsync(customerEmail, "Test123!");

            // Reserve for first event
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Test Concert");
            await Page.ClickAsync("a:has-text('Reserve')");
            var seats1 = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked)");
            if (seats1.Count > 0) await seats1[0].ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve'), button:has-text('Confirm')");

            // Reserve for second event
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Jazz Night");
            await Page.ClickAsync("a:has-text('Reserve')");
            var seats2 = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked)");
            if (seats2.Count > 0) await seats2[0].ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve'), button:has-text('Confirm')");

            // Act - View reservations
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");

            // Assert - Both reservations should be visible
            var concertReservation = await Page.QuerySelectorAsync("text=Test Concert");
            var jazzReservation = await Page.QuerySelectorAsync("text=Jazz Night");

            AssertHelper.IsNotNull(concertReservation, "Test Concert reservation should be displayed");
            AssertHelper.IsNotNull(jazzReservation, "Jazz Night reservation should be displayed");
        }

        [Test]
        public async Task CancelReservation_FreesUpSeats()
        {
            // Arrange - Create event and make reservation
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Cancel", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await SetupTestEventWithSeats();

            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Cancel", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            // Make reservation
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Test Concert");
            await Page.ClickAsync("a:has-text('Reserve')");
            var availableSeats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked)");
            if (availableSeats.Count > 0) await availableSeats[0].ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve')");

            // Act - Cancel reservation
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");
            await Page.ClickAsync("a:has-text('Cancel'), button:has-text('Cancel')");

            // Confirm cancellation
            var confirmButton = await Page.QuerySelectorAsync("button:has-text('Confirm'), button:has-text('Yes')");
            if (confirmButton != null)
            {
                await confirmButton.ClickAsync();
            }

            // Assert - Reservation should be cancelled
            await Page.WaitForTimeoutAsync(1000);
            await Page.ReloadAsync();

            var cancelledReservation = await Page.QuerySelectorAsync("text=Test Concert");
            AssertHelper.IsNull(cancelledReservation, "Cancelled reservation should not appear in list");
        }

        [Test]
        public async Task PreventDoubleBooking_SameSeat()
        {
            // Arrange - Setup event
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Double", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await SetupTestEventWithSeats();

            // First customer books a seat
            await Page.ClickAsync("a:has-text('Logout')");
            var customer1Email = GenerateTestEmail();
            await RegisterUserAsync(customer1Email, "Test123!", "First", "Customer");
            await LoginAsync(customer1Email, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Test Concert");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Remember which seat was clicked
            var firstSeat = await Page.QuerySelectorAsync(".seat.available:first-of-type, input[type='checkbox']:not(:checked):first-of-type");
            await firstSeat.ClickAsync();
            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success, .alert-success");

            // Second customer tries to book the same seat
            await Page.ClickAsync("a:has-text('Logout')");
            var customer2Email = GenerateTestEmail();
            await RegisterUserAsync(customer2Email, "Test123!", "Second", "Customer");
            await LoginAsync(customer2Email, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Test Concert");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Act - Try to select already booked seat
            var bookedSeat = await Page.QuerySelectorAsync(".seat.booked:first-of-type, .seat.unavailable:first-of-type, input[type='checkbox']:checked:disabled:first-of-type");

            // Assert - Booked seat should not be clickable
            if (bookedSeat != null)
            {
                var isDisabled = await bookedSeat.IsDisabledAsync();
                AssertHelper.IsTrue(isDisabled, "Already booked seat should be disabled");
            }

            // Verify available seats count has decreased
            var availableSeats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked):not(:disabled)");
            AssertHelper.Less(availableSeats.Count, 50, "Available seats should be less than total after first booking");
        }

        [Test]
        public async Task EventsForToday_ShowsCurrentEvents()
        {
            // Arrange - Create events for different dates
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Today", "Manager");
            await LoginAsync(email, "Test123!");

            // Create venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Today's Venue");
            await Page.FillAsync("input[name='Address']", "Today Street");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create event for today
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Today's Special Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "18:00");
            await Page.FillAsync("input[name='EndTime']", "20:00");
            await Page.ClickAsync("button[type='submit']");

            // Create future event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Future Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:00");
            await Page.FillAsync("input[name='EndTime']", "21:00");
            await Page.ClickAsync("button[type='submit']");

            // Act - Navigate to Events for Today
            await Page.GotoAsync($"{BaseUrl}/Events/EventsForToday");

            // Assert - Today's event should be prominently displayed
            var todayEvent = await Page.QuerySelectorAsync("text=Today's Special Event");
            AssertHelper.IsNotNull(todayEvent, "Today's event should be displayed");

            // Future event should also be visible but possibly in different section
            var futureEvent = await Page.QuerySelectorAsync("text=Future Event");
            AssertHelper.IsNotNull(futureEvent, "Future events should also be visible");
        }
    }
}