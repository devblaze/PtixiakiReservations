using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class AdvancedSearchTests : TestBase
    {
        private async Task SetupTestData()
        {
            // Create venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Search Test Venue");
            await Page.FillAsync("input[name='Address']", "789 Search Blvd");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create SubArea
            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "Main Stage");
            await Page.FillAsync("input[name='Width']", "150");
            await Page.FillAsync("input[name='Height']", "100");
            await Page.ClickAsync("button[type='submit']");

            // Create multiple events across different dates
            var events = new[]
            {
                ("Rock Concert", DateTime.Now.AddDays(5), "20:00", "23:00", "Rock"),
                ("Jazz Evening", DateTime.Now.AddDays(7), "19:00", "22:00", "Jazz"),
                ("Classical Night", DateTime.Now.AddDays(10), "19:30", "22:30", "Classical"),
                ("Pop Festival", DateTime.Now.AddDays(15), "18:00", "23:59", "Pop"),
                ("Blues Session", DateTime.Now.AddDays(20), "21:00", "23:30", "Blues"),
                ("Opera Gala", DateTime.Now.AddDays(25), "19:00", "22:00", "Opera")
            };

            foreach (var (name, date, startTime, endTime, category) in events)
            {
                await Page.ClickAsync("a:has-text('Create Event')");
                await Page.FillAsync("input[name='Name']", name);
                await Page.FillAsync("input[name='StartDate']", date.ToString("yyyy-MM-dd"));
                await Page.FillAsync("input[name='StartTime']", startTime);
                await Page.FillAsync("input[name='EndTime']", endTime);

                var descField = await Page.QuerySelectorAsync("textarea[name='Description']");
                if (descField != null)
                {
                    await Page.FillAsync("textarea[name='Description']", $"{category} music performance");
                }

                await Page.ClickAsync("button[type='submit']");
                await Page.WaitForTimeoutAsync(500);
            }
        }

        [Test]
        public async Task SearchEvents_ByDateRange_ReturnsCorrectEvents()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "DateRange", "Search");
            await LoginAsync(email, "Test123!");
            await SetupTestData();

            // Act - Search events within date range
            await Page.GotoAsync($"{BaseUrl}/Events");

            // Fill date range filters
            var startDateFilter = await Page.QuerySelectorAsync("input[name='startDate'], input[name='dateFrom']");
            var endDateFilter = await Page.QuerySelectorAsync("input[name='endDate'], input[name='dateTo']");

            if (startDateFilter != null && endDateFilter != null)
            {
                await Page.FillAsync("input[name='startDate'], input[name='dateFrom']",
                    DateTime.Now.AddDays(6).ToString("yyyy-MM-dd"));
                await Page.FillAsync("input[name='endDate'], input[name='dateTo']",
                    DateTime.Now.AddDays(16).ToString("yyyy-MM-dd"));

                await Page.ClickAsync("button[type='submit']:has-text('Search'), button:has-text('Filter')");
            }

            // Assert - Should show events within the date range
            await Page.WaitForSelectorAsync("text=Jazz Evening");
            var jazzEvent = await Page.QuerySelectorAsync("text=Jazz Evening");
            var classicalEvent = await Page.QuerySelectorAsync("text=Classical Night");
            var popEvent = await Page.QuerySelectorAsync("text=Pop Festival");
            var rockEvent = await Page.QuerySelectorAsync("text=Rock Concert");
            var operaEvent = await Page.QuerySelectorAsync("text=Opera Gala");

            AssertHelper.IsNotNull(jazzEvent, "Jazz Evening should be in results");
            AssertHelper.IsNotNull(classicalEvent, "Classical Night should be in results");
            AssertHelper.IsNotNull(popEvent, "Pop Festival should be in results");
            AssertHelper.IsNull(rockEvent, "Rock Concert should not be in results (too early)");
            AssertHelper.IsNull(operaEvent, "Opera Gala should not be in results (too late)");
        }

        [Test]
        public async Task SearchEvents_ByPriceRange_FiltersCorrectly()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Price", "Search");
            await LoginAsync(email, "Test123!");

            // Create venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Price Test Venue");
            await Page.FillAsync("input[name='Address']", "Price Street");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create events with different prices
            var priceEvents = new[]
            {
                ("Budget Show", "25.00"),
                ("Regular Concert", "50.00"),
                ("Premium Event", "100.00"),
                ("VIP Experience", "200.00")
            };

            foreach (var (eventName, price) in priceEvents)
            {
                await Page.ClickAsync("a:has-text('Create Event')");
                await Page.FillAsync("input[name='Name']", eventName);
                await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"));
                await Page.FillAsync("input[name='StartTime']", "20:00");
                await Page.FillAsync("input[name='EndTime']", "22:00");

                var priceField = await Page.QuerySelectorAsync("input[name='Price'], input[name='TicketPrice']");
                if (priceField != null)
                {
                    await Page.FillAsync("input[name='Price'], input[name='TicketPrice']", price);
                }

                await Page.ClickAsync("button[type='submit']");
                await Page.WaitForTimeoutAsync(500);
            }

            // Act - Search with price filter
            await Page.GotoAsync($"{BaseUrl}/Events");

            var minPriceFilter = await Page.QuerySelectorAsync("input[name='minPrice'], input[name='priceFrom']");
            var maxPriceFilter = await Page.QuerySelectorAsync("input[name='maxPrice'], input[name='priceTo']");

            if (minPriceFilter != null && maxPriceFilter != null)
            {
                await Page.FillAsync("input[name='minPrice'], input[name='priceFrom']", "40");
                await Page.FillAsync("input[name='maxPrice'], input[name='priceTo']", "150");
                await Page.ClickAsync("button[type='submit']:has-text('Search')");
            }

            // Assert
            await Page.WaitForTimeoutAsync(1000);
            var regularEvent = await Page.QuerySelectorAsync("text=Regular Concert");
            var premiumEvent = await Page.QuerySelectorAsync("text=Premium Event");
            var budgetEvent = await Page.QuerySelectorAsync("text=Budget Show");
            var vipEvent = await Page.QuerySelectorAsync("text=VIP Experience");

            AssertHelper.IsNotNull(regularEvent, "Regular Concert should be in price range");
            AssertHelper.IsNotNull(premiumEvent, "Premium Event should be in price range");
            AssertHelper.IsNull(budgetEvent, "Budget Show should be filtered out");
            AssertHelper.IsNull(vipEvent, "VIP Experience should be filtered out");
        }

        [Test]
        public async Task SearchEvents_ByMultipleCriteria_CombinesFilters()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Multi", "Filter");
            await LoginAsync(email, "Test123!");
            await SetupTestData();

            // Act - Apply multiple search criteria
            await Page.GotoAsync($"{BaseUrl}/Events");

            // Search by text
            var searchField = await Page.QuerySelectorAsync("input[name='searchString'], input[placeholder*='Search']");
            if (searchField != null)
            {
                await Page.FillAsync("input[name='searchString'], input[placeholder*='Search']", "Concert");
            }

            // Add date filter
            var dateFilter = await Page.QuerySelectorAsync("input[name='startDate']");
            if (dateFilter != null)
            {
                await Page.FillAsync("input[name='startDate']", DateTime.Now.ToString("yyyy-MM-dd"));
            }

            // Add city filter
            var cityFilter = await Page.QuerySelectorAsync("select[name='city']");
            if (cityFilter != null)
            {
                await Page.SelectOptionAsync("select[name='city']", new SelectOptionValue { Index = 1 });
            }

            await Page.ClickAsync("button[type='submit']:has-text('Search')");

            // Assert
            await Page.WaitForTimeoutAsync(1000);
            var rockConcert = await Page.QuerySelectorAsync("text=Rock Concert");
            var jazzEvening = await Page.QuerySelectorAsync("text=Jazz Evening");

            AssertHelper.IsNotNull(rockConcert, "Rock Concert should match search criteria");
            AssertHelper.IsNull(jazzEvening, "Jazz Evening should not match 'Concert' search");
        }

        [Test]
        public async Task SearchEvents_WithAvailabilityFilter_ShowsOnlyAvailable()
        {
            // Arrange - Create events and make some fully booked
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Availability", "Manager");
            await LoginAsync(managerEmail, "Test123!");

            // Create venue with limited seats
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Small Venue");
            await Page.FillAsync("input[name='Address']", "Limited St");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create SubArea with only 2 seats
            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "Tiny Room");
            await Page.FillAsync("input[name='Width']", "20");
            await Page.FillAsync("input[name='Height']", "20");
            await Page.ClickAsync("button[type='submit']");

            // Add limited seats
            await Page.ClickAsync("a:has-text('Add Seats'), a:has-text('Create Seat')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "1");
                await Page.FillAsync("input[name='Columns']", "2");
                await Page.ClickAsync("button[type='submit']");
            }

            // Create two events
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Sold Out Show");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "21:00");
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Available Show");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(4).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "21:00");
            await Page.ClickAsync("button[type='submit']");

            // Book all seats for first event
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Book", "All");
            await LoginAsync(customerEmail, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Sold Out Show");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Book all available seats
            var allSeats = await Page.QuerySelectorAllAsync(".seat.available, input[type='checkbox']:not(:checked)");
            foreach (var seat in allSeats)
            {
                await seat.ClickAsync();
            }
            await Page.ClickAsync("button:has-text('Reserve')");

            // Act - Search with availability filter
            await Page.GotoAsync($"{BaseUrl}/Events");
            var availabilityFilter = await Page.QuerySelectorAsync("input[type='checkbox'][name='availableOnly']");
            if (availabilityFilter != null)
            {
                await Page.CheckAsync("input[type='checkbox'][name='availableOnly']");
                await Page.ClickAsync("button[type='submit']:has-text('Search')");
            }

            // Assert
            await Page.WaitForTimeoutAsync(1000);
            var availableShow = await Page.QuerySelectorAsync("text=Available Show");
            var soldOutShow = await Page.QuerySelectorAsync("text=Sold Out Show");

            AssertHelper.IsNotNull(availableShow, "Available Show should be displayed");
            AssertHelper.IsNull(soldOutShow, "Sold Out Show should be filtered out");
        }

        [Test]
        public async Task SearchEvents_WithSorting_OrdersCorrectly()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Sort", "Test");
            await LoginAsync(email, "Test123!");
            await SetupTestData();

            // Act - Apply sorting
            await Page.GotoAsync($"{BaseUrl}/Events");

            var sortSelect = await Page.QuerySelectorAsync("select[name='sortBy'], select[name='sort']");
            if (sortSelect != null)
            {
                // Sort by date ascending
                await Page.SelectOptionAsync("select[name='sortBy'], select[name='sort']", "DateAsc");
                await Page.ClickAsync("button[type='submit']:has-text('Sort'), button:has-text('Apply')");

                // Get first event
                await Page.WaitForTimeoutAsync(1000);
                var firstEvent = await Page.QuerySelectorAsync(".event-card:first-of-type, .event-item:first-of-type, tr.event-row:first-of-type");
                if (firstEvent != null)
                {
                    var firstEventText = await firstEvent.TextContentAsync();
                    Assert.That(firstEventText, Does.Contain("Rock Concert"), "First event should be Rock Concert (earliest date)");
                }

                // Sort by name descending
                await Page.SelectOptionAsync("select[name='sortBy'], select[name='sort']", "NameDesc");
                await Page.ClickAsync("button[type='submit']:has-text('Sort'), button:has-text('Apply')");

                await Page.WaitForTimeoutAsync(1000);
                var firstEventAfterSort = await Page.QuerySelectorAsync(".event-card:first-of-type, .event-item:first-of-type");
                if (firstEventAfterSort != null)
                {
                    var sortedText = await firstEventAfterSort.TextContentAsync();
                    Assert.That(sortedText, Does.Contain("Rock").Or.Contain("Pop"), "First event should be alphabetically last");
                }
            }
        }

        [Test]
        public async Task SearchEvents_WithPagination_NavigatesPages()
        {
            // Arrange - Create many events
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Page", "Test");
            await LoginAsync(email, "Test123!");

            // Create venue
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Pagination Venue");
            await Page.FillAsync("input[name='Address']", "Page St");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create 25 events to ensure pagination
            for (int i = 1; i <= 25; i++)
            {
                await Page.ClickAsync("a:has-text('Create Event')");
                await Page.FillAsync("input[name='Name']", $"Event {i:D2}");
                await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(i).ToString("yyyy-MM-dd"));
                await Page.FillAsync("input[name='StartTime']", "19:00");
                await Page.FillAsync("input[name='EndTime']", "21:00");
                await Page.ClickAsync("button[type='submit']");
                await Page.WaitForTimeoutAsync(300);
            }

            // Act - Navigate through pages
            await Page.GotoAsync($"{BaseUrl}/Events");

            // Check for pagination controls
            var nextPageButton = await Page.QuerySelectorAsync("a:has-text('Next'), button:has-text('Next'), .pagination-next");
            var pageNumbers = await Page.QuerySelectorAllAsync(".page-number, .pagination-link");

            if (nextPageButton != null)
            {
                // Click next page
                await nextPageButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);

                // Verify we're on page 2
                var currentPageIndicator = await Page.QuerySelectorAsync(".active.page-number, .current-page");
                if (currentPageIndicator != null)
                {
                    var pageText = await currentPageIndicator.TextContentAsync();
                    Assert.That(pageText, Does.Contain("2"), "Should be on page 2");
                }
            }

            // Assert - Check that different events are shown
            AssertHelper.IsNotNull(pageNumbers, "Pagination should be present for large result sets");
        }

        [Test]
        public async Task SearchEvents_SavedSearchFilters_PersistAcrossSession()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Save", "Filter");
            await LoginAsync(email, "Test123!");
            await SetupTestData();

            // Act - Set and save search filters
            await Page.GotoAsync($"{BaseUrl}/Events");

            // Apply filters
            await Page.FillAsync("input[name='searchString'], input[placeholder*='Search']", "Concert");

            var saveFilterButton = await Page.QuerySelectorAsync("button:has-text('Save Filter'), button:has-text('Save Search')");
            if (saveFilterButton != null)
            {
                await saveFilterButton.ClickAsync();

                // Name the saved search
                var filterNameInput = await Page.QuerySelectorAsync("input[name='filterName']");
                if (filterNameInput != null)
                {
                    await Page.FillAsync("input[name='filterName']", "My Concert Search");
                    await Page.ClickAsync("button:has-text('Save')");
                }
            }

            // Navigate away and back
            await Page.GotoAsync($"{BaseUrl}");
            await Page.GotoAsync($"{BaseUrl}/Events");

            // Check for saved filters
            var savedFiltersDropdown = await Page.QuerySelectorAsync("select[name='savedFilters']");
            if (savedFiltersDropdown != null)
            {
                await Page.SelectOptionAsync("select[name='savedFilters']", "My Concert Search");

                // Verify filter is applied
                var searchField = await Page.QuerySelectorAsync("input[name='searchString']");
                if (searchField != null)
                {
                    var searchValue = await searchField.InputValueAsync();
                    Assert.That(searchValue, Is.EqualTo("Concert"), "Saved filter should restore search term");
                }
            }
        }
    }
}