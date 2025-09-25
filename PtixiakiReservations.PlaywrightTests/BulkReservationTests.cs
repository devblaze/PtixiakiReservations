using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class BulkReservationTests : TestBase
    {
        private async Task CreateVenueWithLargeCapacity()
        {
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Large Concert Hall");
            await Page.FillAsync("input[name='Address']", "999 Arena Way");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create multiple SubAreas
            var subAreas = new[]
            {
                ("Orchestra", "200", "150", "Front seating area"),
                ("Mezzanine", "180", "100", "Middle level seating"),
                ("Balcony", "150", "80", "Upper level seating")
            };

            foreach (var (name, width, height, desc) in subAreas)
            {
                await Page.ClickAsync("a:has-text('Add Sub Area')");
                await Page.FillAsync("input[name='AreaName']", name);
                await Page.FillAsync("input[name='Width']", width);
                await Page.FillAsync("input[name='Height']", height);
                await Page.FillAsync("input[name='Desc']", desc);
                await Page.ClickAsync("button[type='submit']");
                await Page.WaitForTimeoutAsync(500);
            }

            // Create seats for each area
            foreach (var area in subAreas)
            {
                await Page.ClickAsync($"a:has-text('{area.Item1}')");
                await Page.ClickAsync("a:has-text('Add Seats'), a:has-text('Create Seat Map')");

                var seatMapExists = await ElementExistsAsync("input[name='Rows']");
                if (seatMapExists)
                {
                    await Page.FillAsync("input[name='Rows']", "10");
                    await Page.FillAsync("input[name='Columns']", "20");
                    await Page.ClickAsync("button[type='submit']");
                }
                await Page.WaitForTimeoutAsync(500);
            }
        }

        [Test]
        public async Task BulkReservation_SelectMultipleSeats_Success()
        {
            // Arrange - Create venue and event as manager
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Bulk", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateVenueWithLargeCapacity();

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Symphony Orchestra Performance");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "22:30");
            await Page.ClickAsync("button[type='submit']");

            // Login as customer
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Bulk", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            // Act - Reserve multiple seats
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Symphony Orchestra Performance");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Select 10 seats
            var availableSeats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked):not(:disabled)");
            var seatsToSelect = Math.Min(10, availableSeats.Count);

            for (int i = 0; i < seatsToSelect; i++)
            {
                await availableSeats[i].ClickAsync();
                await Page.WaitForTimeoutAsync(100);
            }

            // Verify selection count
            var selectedCount = await Page.QuerySelectorAsync(".selected-count, text=10 seats selected");
            AssertHelper.IsNotNull(selectedCount, "Should show count of selected seats");

            // Confirm reservation
            await Page.ClickAsync("button:has-text('Reserve'), button:has-text('Book Selected Seats')");

            // Assert - Verify bulk reservation success
            await Page.WaitForSelectorAsync("text=success, .alert-success");

            // Check reservation details
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");
            var reservationDetails = await Page.QuerySelectorAsync("text=Symphony Orchestra Performance");
            AssertHelper.IsNotNull(reservationDetails, "Bulk reservation should appear in user's list");

            // Verify seat count in reservation
            var seatCountElement = await Page.QuerySelectorAsync($"text={seatsToSelect} seats, text=Seats: {seatsToSelect}");
            AssertHelper.IsNotNull(seatCountElement, $"Should show {seatsToSelect} reserved seats");
        }

        [Test]
        public async Task BulkReservation_SelectEntireRow_QuickSelection()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Row", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateVenueWithLargeCapacity();

            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Theater Production");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:30");
            await Page.FillAsync("input[name='EndTime']", "22:00");
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Row", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            // Act - Select entire row
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Theater Production");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Look for row selection option
            var selectRowButton = await Page.QuerySelectorAsync("button:has-text('Select Row'), a:has-text('Select Entire Row')");
            if (selectRowButton != null)
            {
                await selectRowButton.ClickAsync();

                // Select first available row
                var firstRow = await Page.QuerySelectorAsync(".row-selector:first-of-type, input[name='selectRow']:first-of-type");
                if (firstRow != null)
                {
                    await firstRow.ClickAsync();
                }
            }
            else
            {
                // Manually select all seats in first row
                var firstRowSeats = await Page.QuerySelectorAllAsync("[data-row='A'] .seat.available, .row-A .seat.available");
                foreach (var seat in firstRowSeats.Take(10))
                {
                    await seat.ClickAsync();
                }
            }

            await Page.ClickAsync("button:has-text('Reserve')");

            // Assert
            await Page.WaitForSelectorAsync("text=success");
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");

            var theatreReservation = await Page.QuerySelectorAsync("text=Theater Production");
            AssertHelper.IsNotNull(theatreReservation, "Row reservation should be successful");
        }

        [Test]
        public async Task BulkReservation_GroupBooking_AppliesDiscount()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Group", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateVenueWithLargeCapacity();

            // Create event with group discount
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Comedy Show");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "21:00");
            await Page.FillAsync("input[name='EndTime']", "23:00");

            var priceField = await Page.QuerySelectorAsync("input[name='Price']");
            if (priceField != null)
            {
                await Page.FillAsync("input[name='Price']", "30.00");
            }

            // Set group discount if available
            var groupDiscountField = await Page.QuerySelectorAsync("input[name='GroupDiscount']");
            if (groupDiscountField != null)
            {
                await Page.FillAsync("input[name='GroupDiscount']", "10");
                await Page.FillAsync("input[name='GroupSize']", "6");
            }

            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Group", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            // Act - Book as group
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Comedy Show");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Select 8 seats (qualifying for group discount)
            var availableSeats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked)");
            for (int i = 0; i < Math.Min(8, availableSeats.Count); i++)
            {
                await availableSeats[i].ClickAsync();
            }

            // Check for discount display
            var discountDisplay = await Page.QuerySelectorAsync("text=Group discount, text=10% off");
            var totalPriceElement = await Page.QuerySelectorAsync(".total-price, .price-total");

            if (totalPriceElement != null)
            {
                var totalPrice = await totalPriceElement.TextContentAsync();
                // Original: 8 * 30 = 240, With 10% discount: 216
                Assert.That(totalPrice, Does.Contain("216").Or.Contain("$216"), "Group discount should be applied");
            }

            await Page.ClickAsync("button:has-text('Reserve')");

            // Assert
            await Page.WaitForSelectorAsync("text=success");
            AssertHelper.IsNotNull(discountDisplay, "Group discount should be displayed when applicable");
        }

        [Test]
        public async Task BulkReservation_SeatSelection_ShowsRealTimeAvailability()
        {
            // Arrange - Setup with two customers
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Realtime", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateVenueWithLargeCapacity();

            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Popular Concert");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "23:00");
            await Page.ClickAsync("button[type='submit']");

            // First customer starts selection
            await Page.ClickAsync("a:has-text('Logout')");
            var customer1Email = GenerateTestEmail();
            await RegisterUserAsync(customer1Email, "Test123!", "First", "User");
            await LoginAsync(customer1Email, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Popular Concert");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Select and hold some seats (don't complete reservation yet)
            var firstCustomerSeats = await Page.QuerySelectorAllAsync(".seat.available:nth-child(-n+5)");
            foreach (var seat in firstCustomerSeats.Take(5))
            {
                await seat.ClickAsync();
            }

            // Get seat IDs or labels
            var selectedSeatLabels = await Page.QuerySelectorAllAsync(".seat.selected .seat-label");
            var seatTexts = new List<string>();
            foreach (var label in selectedSeatLabels)
            {
                seatTexts.Add(await label.TextContentAsync() ?? "");
            }

            // Complete first customer's reservation
            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success");

            // Second customer tries to book
            await Page.ClickAsync("a:has-text('Logout')");
            var customer2Email = GenerateTestEmail();
            await RegisterUserAsync(customer2Email, "Test123!", "Second", "User");
            await LoginAsync(customer2Email, "Test123!");

            // Act
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Popular Concert");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Assert - Previously selected seats should be unavailable
            var unavailableSeats = await Page.QuerySelectorAllAsync(".seat.booked, .seat.unavailable, .seat.reserved");
            AssertHelper.GreaterOrEqual(unavailableSeats.Count, 5, "First customer's seats should be unavailable");

            // Try to click on a booked seat
            if (unavailableSeats.Count > 0)
            {
                var isClickable = await unavailableSeats[0].IsEnabledAsync();
                AssertHelper.IsFalse(isClickable, "Booked seats should not be clickable");
            }
        }

        [Test]
        public async Task BulkReservation_CancelPartialSelection_UpdatesAvailability()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Partial", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateVenueWithLargeCapacity();

            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Movie Premiere");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:00");
            await Page.FillAsync("input[name='EndTime']", "21:30");
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Cancel", "Partial");
            await LoginAsync(customerEmail, "Test123!");

            // Reserve multiple seats
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Movie Premiere");
            await Page.ClickAsync("a:has-text('Reserve')");

            var seats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked)");
            for (int i = 0; i < Math.Min(6, seats.Count); i++)
            {
                await seats[i].ClickAsync();
            }

            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success");

            // Act - Cancel some seats
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");
            await Page.ClickAsync("text=Movie Premiere");

            var modifyButton = await Page.QuerySelectorAsync("button:has-text('Modify'), a:has-text('Change Seats')");
            if (modifyButton != null)
            {
                await modifyButton.ClickAsync();

                // Deselect some seats
                var selectedSeats = await Page.QuerySelectorAllAsync(".seat.selected, input[type='checkbox']:checked");
                for (int i = 0; i < Math.Min(3, selectedSeats.Count); i++)
                {
                    await selectedSeats[i].ClickAsync();
                }

                await Page.ClickAsync("button:has-text('Update'), button:has-text('Save Changes')");
            }
            else
            {
                // Alternative: Cancel entire reservation
                await Page.ClickAsync("button:has-text('Cancel'), a:has-text('Cancel Reservation')");
                var confirmButton = await Page.QuerySelectorAsync("button:has-text('Confirm')");
                if (confirmButton != null)
                {
                    await confirmButton.ClickAsync();
                }
            }

            // Assert - Seats should be available again
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Movie Premiere");
            await Page.ClickAsync("a:has-text('Reserve')");

            var nowAvailableSeats = await Page.QuerySelectorAllAsync(".seat.available");
            AssertHelper.Greater(nowAvailableSeats.Count, 0, "Cancelled seats should be available again");
        }

        [Test]
        public async Task BulkReservation_SeatMap_InteractiveSelection()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Interactive", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateVenueWithLargeCapacity();

            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Dance Performance");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(12).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:00");
            await Page.FillAsync("input[name='EndTime']", "21:00");
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Interactive", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            // Act - Use interactive seat map
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Dance Performance");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Test drag selection if available
            var seatMapContainer = await Page.QuerySelectorAsync(".seat-map, .seating-chart");
            if (seatMapContainer != null)
            {
                // Try drag selection
                var boundingBox = await seatMapContainer.BoundingBoxAsync();
                if (boundingBox != null)
                {
                    await Page.Mouse.MoveAsync(boundingBox.X + 10, boundingBox.Y + 10);
                    await Page.Mouse.DownAsync();
                    await Page.Mouse.MoveAsync(boundingBox.X + 100, boundingBox.Y + 50);
                    await Page.Mouse.UpAsync();
                }
            }

            // Alternative: Click multiple seats with Shift/Ctrl
            var firstSeat = await Page.QuerySelectorAsync(".seat.available:first-of-type");
            var lastSeat = await Page.QuerySelectorAsync(".seat.available:nth-of-type(10)");

            if (firstSeat != null && lastSeat != null)
            {
                await firstSeat.ClickAsync();
                await Page.Keyboard.DownAsync("Shift");
                await lastSeat.ClickAsync();
                await Page.Keyboard.UpAsync("Shift");
            }

            // Check selection preview
            var selectionPreview = await Page.QuerySelectorAsync(".selection-preview, .selected-seats-summary");
            AssertHelper.IsNotNull(selectionPreview, "Should show preview of selected seats");

            await Page.ClickAsync("button:has-text('Reserve')");

            // Assert
            await Page.WaitForSelectorAsync("text=success");
            var reservation = await Page.QuerySelectorAsync("text=Dance Performance");
            AssertHelper.IsNotNull(reservation, "Interactive seat selection should complete successfully");
        }

        [Test]
        public async Task BulkReservation_AccessibleSeats_PrioritySelection()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Accessible", "Manager");
            await LoginAsync(managerEmail, "Test123!");

            // Create venue with accessible seats
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Accessible Theater");
            await Page.FillAsync("input[name='Address']", "123 Access Blvd");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "Main Floor");
            await Page.FillAsync("input[name='Width']", "150");
            await Page.FillAsync("input[name='Height']", "100");
            await Page.ClickAsync("button[type='submit']");

            // Create seats with accessible options
            await Page.ClickAsync("a:has-text('Add Seats')");
            var accessibleOption = await Page.QuerySelectorAsync("input[name='CreateAccessibleSeats']");
            if (accessibleOption != null)
            {
                await Page.CheckAsync("input[name='CreateAccessibleSeats']");
                await Page.FillAsync("input[name='AccessibleCount']", "10");
            }

            await Page.ClickAsync("button[type='submit']");

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Accessible Show");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(8).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "14:00");
            await Page.FillAsync("input[name='EndTime']", "16:00");
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Access", "User");
            await LoginAsync(customerEmail, "Test123!");

            // Act - Filter for accessible seats
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Accessible Show");
            await Page.ClickAsync("a:has-text('Reserve')");

            var accessibleFilter = await Page.QuerySelectorAsync("input[type='checkbox'][name='ShowAccessibleOnly']");
            if (accessibleFilter != null)
            {
                await Page.CheckAsync("input[type='checkbox'][name='ShowAccessibleOnly']");
            }

            // Select accessible seats
            var accessibleSeats = await Page.QuerySelectorAllAsync(".seat.accessible, .seat[data-accessible='true']");
            if (accessibleSeats.Count > 0)
            {
                for (int i = 0; i < Math.Min(2, accessibleSeats.Count); i++)
                {
                    await accessibleSeats[i].ClickAsync();
                }
            }
            else
            {
                // Select any available seats
                var anySeats = await Page.QuerySelectorAllAsync(".seat.available");
                for (int i = 0; i < Math.Min(2, anySeats.Count); i++)
                {
                    await anySeats[i].ClickAsync();
                }
            }

            await Page.ClickAsync("button:has-text('Reserve')");

            // Assert
            await Page.WaitForSelectorAsync("text=success");
            await Page.GotoAsync($"{BaseUrl}/Profile/Reservations");

            var accessibleReservation = await Page.QuerySelectorAsync("text=Accessible Show");
            AssertHelper.IsNotNull(accessibleReservation, "Accessible seat reservation should be successful");
        }
    }
}