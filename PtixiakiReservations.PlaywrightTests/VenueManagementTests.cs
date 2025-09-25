using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class VenueManagementTests : TestBase
    {
        [Test]
        public async Task CreateVenueWithSubAreaAndSeats_CompleteWorkflow()
        {
            // Arrange - Register and login as venue manager
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Venue", "Manager");
            await LoginAsync(email, "Test123!");

            // Navigate to venue creation page
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");

            // Act - Fill venue creation form
            await Page.FillAsync("input[name='Name']", "Playwright Theater");
            await Page.FillAsync("input[name='Address']", "123 Test Street");
            await Page.FillAsync("input[name='PostalCode']", "12345");
            await Page.FillAsync("input[name='Phone']", "1234567890");

            // Select city from dropdown
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });

            // Upload image if file input exists
            var fileInput = await Page.QuerySelectorAsync("input[type='file'][name='ImageFile']");
            if (fileInput != null)
            {
                // Create a test image or skip
                // await fileInput.SetInputFilesAsync("path/to/test/image.jpg");
            }

            // Submit form
            await Page.ClickAsync("button[type='submit'], input[type='submit']");

            // Wait for navigation to venue list or details
            await Page.WaitForURLAsync(url => url.Contains("/Venue/") && !url.Contains("/Create"));

            // Assert - Verify venue was created
            await Page.WaitForSelectorAsync("text=Playwright Theater");
            var venueNameElement = await Page.QuerySelectorAsync("h1:has-text('Playwright Theater'), h2:has-text('Playwright Theater')");
            AssertHelper.IsNotNull(venueNameElement, "Venue name should be displayed");

            // Create SubArea for the venue
            await Page.ClickAsync("a:has-text('Add Sub Area'), button:has-text('Add Sub Area')");

            // Fill SubArea form
            await Page.FillAsync("input[name='AreaName']", "Main Hall");
            await Page.FillAsync("input[name='Width']", "100");
            await Page.FillAsync("input[name='Height']", "80");
            await Page.FillAsync("input[name='Desc']", "Main seating area");

            // Submit SubArea form
            await Page.ClickAsync("button[type='submit']:has-text('Create'), button[type='submit']:has-text('Save')");

            // Wait for SubArea to be created
            await Page.WaitForSelectorAsync("text=Main Hall");

            // Navigate to seat creation
            await Page.ClickAsync("a:has-text('Add Seats'), button:has-text('Add Seats'), a:has-text('Create Seat Map')");

            // Create seats using seat map or individual seats
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");

            if (seatMapExists)
            {
                // Use seat map creation
                await Page.FillAsync("input[name='Rows']", "5");
                await Page.FillAsync("input[name='Columns']", "10");
                await Page.FillAsync("input[name='StartingRow']", "A");
                await Page.FillAsync("input[name='StartingNumber']", "1");

                await Page.ClickAsync("button[type='submit']:has-text('Create'), button[type='submit']:has-text('Generate')");
            }
            else
            {
                // Create individual seats
                for (int i = 1; i <= 5; i++)
                {
                    await Page.FillAsync("input[name='Name']", $"A{i}");
                    await Page.FillAsync("input[name='X']", $"{10 + i * 5}");
                    await Page.FillAsync("input[name='Y']", "20");
                    await Page.CheckAsync("input[name='Available']");

                    await Page.ClickAsync("button[type='submit']:has-text('Add Seat')");
                    await Page.WaitForTimeoutAsync(500); // Small delay between seat creations
                }
            }

            // Assert - Verify seats were created
            var seatElements = await Page.QuerySelectorAllAsync(".seat, div[data-seat], tr:has-text('A1')");
            AssertHelper.Greater(seatElements.Count, 0, "Seats should be created and displayed");
        }

        [Test]
        public async Task EditVenue_UpdatesInformation()
        {
            // Arrange - Login as venue manager
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Editor", "User");
            await LoginAsync(email, "Test123!");

            // First create a venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Original Venue");
            await Page.FillAsync("input[name='Address']", "Original Address");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Navigate to edit page
            await Page.ClickAsync("a:has-text('Edit')");

            // Act - Update venue information
            await Page.FillAsync("input[name='Name']", "Updated Venue Name");
            await Page.FillAsync("input[name='Address']", "Updated Address 456");
            await Page.FillAsync("input[name='PostalCode']", "54321");
            await Page.FillAsync("input[name='Phone']", "9876543210");

            await Page.ClickAsync("button[type='submit']:has-text('Save'), button[type='submit']:has-text('Update')");

            // Assert - Verify updates
            await Page.WaitForSelectorAsync("text=Updated Venue Name");
            var updatedName = await Page.TextContentAsync("h1, h2, .venue-name");
            Assert.That(updatedName, Does.Contain("Updated Venue Name"));
        }

        [Test]
        public async Task ViewMyVenues_DisplaysVenueList()
        {
            // Arrange - Login as venue manager
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Manager", "User");
            await LoginAsync(email, "Test123!");

            // Create multiple venues
            for (int i = 1; i <= 3; i++)
            {
                await Page.GotoAsync($"{BaseUrl}/Venue/Create");
                await Page.FillAsync("input[name='Name']", $"Test Venue {i}");
                await Page.FillAsync("input[name='Address']", $"Address {i}");
                await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
                await Page.ClickAsync("button[type='submit']");
                await Page.WaitForTimeoutAsync(500);
            }

            // Act - Navigate to My Venues
            await Page.GotoAsync($"{BaseUrl}/Venue/MyVenues");

            // Assert - Verify all venues are displayed
            for (int i = 1; i <= 3; i++)
            {
                var venueElement = await Page.QuerySelectorAsync($"text=Test Venue {i}");
                AssertHelper.IsNotNull(venueElement, $"Venue {i} should be displayed");
            }

            // Verify venue count
            var venueCards = await Page.QuerySelectorAllAsync(".venue-card, .card, tr.venue-row");
            AssertHelper.GreaterOrEqual(venueCards.Count, 3, "Should display at least 3 venues");
        }

        [Test]
        public async Task DeleteVenue_RemovesVenue()
        {
            // Arrange - Create and login
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Delete", "Tester");
            await LoginAsync(email, "Test123!");

            // Create a venue to delete
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Venue To Delete");
            await Page.FillAsync("input[name='Address']", "Delete Address");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Act - Delete the venue
            await Page.ClickAsync("a:has-text('Delete'), button:has-text('Delete')");

            // Confirm deletion if there's a confirmation dialog
            var confirmButton = await Page.QuerySelectorAsync("button:has-text('Confirm'), button:has-text('Yes')");
            if (confirmButton != null)
            {
                await confirmButton.ClickAsync();
            }

            // Assert - Verify venue is deleted
            await Page.GotoAsync($"{BaseUrl}/Venue/MyVenues");
            var deletedVenue = await Page.QuerySelectorAsync("text=Venue To Delete");
            AssertHelper.IsNull(deletedVenue, "Deleted venue should not be displayed");
        }

        [Test]
        public async Task CreateMultipleSubAreas_Success()
        {
            // Arrange - Create venue
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "SubArea", "Manager");
            await LoginAsync(email, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Multi-Area Venue");
            await Page.FillAsync("input[name='Address']", "Multi Address");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Act - Create multiple SubAreas
            string[] subAreas = { "Orchestra", "Balcony", "VIP Box" };

            foreach (var areaName in subAreas)
            {
                await Page.ClickAsync("a:has-text('Add Sub Area'), button:has-text('Add Sub Area')");

                await Page.FillAsync("input[name='AreaName']", areaName);
                await Page.FillAsync("input[name='Width']", "50");
                await Page.FillAsync("input[name='Height']", "40");
                await Page.FillAsync("input[name='Desc']", $"{areaName} seating area");

                await Page.ClickAsync("button[type='submit']");
                await Page.WaitForTimeoutAsync(500);
            }

            // Assert - Verify all SubAreas are created
            foreach (var areaName in subAreas)
            {
                var areaElement = await Page.QuerySelectorAsync($"text={areaName}");
                AssertHelper.IsNotNull(areaElement, $"{areaName} should be displayed");
            }

            // Verify SubArea count
            var subAreaElements = await Page.QuerySelectorAllAsync(".subarea, .area-card, tr.subarea-row");
            AssertHelper.GreaterOrEqual(subAreaElements.Count, 3, "Should have at least 3 SubAreas");
        }
    }
}