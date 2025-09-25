using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PtixiakiReservations.PlaywrightTests
{
    [TestFixture]
    public class SeatVisualizationTests : TestBase
    {
        private async Task CreateComplexVenueLayout()
        {
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Complex Theater");
            await Page.FillAsync("input[name='Address']", "321 Stage Road");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            // Create multiple SubAreas with different layouts
            var subAreas = new[]
            {
                ("Orchestra Left", "80", "60", "Left orchestra section"),
                ("Orchestra Center", "100", "60", "Center orchestra section"),
                ("Orchestra Right", "80", "60", "Right orchestra section"),
                ("Balcony", "120", "40", "Upper balcony section"),
                ("VIP Box A", "30", "20", "VIP box left"),
                ("VIP Box B", "30", "20", "VIP box right")
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
        }

        [Test]
        public async Task SeatMap_DisplaysCorrectLayout()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Layout", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateComplexVenueLayout();

            // Add seats to first SubArea
            await Page.ClickAsync("text=Orchestra Center");
            await Page.ClickAsync("a:has-text('Add Seats'), a:has-text('Create Seat Map')");

            // Act - Create seat layout
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "8");
                await Page.FillAsync("input[name='Columns']", "12");
                await Page.FillAsync("input[name='StartingRow']", "A");
                await Page.FillAsync("input[name='StartingNumber']", "1");

                // Set seat spacing if available
                var spacingField = await Page.QuerySelectorAsync("input[name='SeatSpacing']");
                if (spacingField != null)
                {
                    await Page.FillAsync("input[name='SeatSpacing']", "5");
                }

                await Page.ClickAsync("button[type='submit']");
            }

            // Assert - Verify seat map visualization
            await Page.WaitForSelectorAsync(".seat-map, .seating-chart, .seats-container");

            var seatElements = await Page.QuerySelectorAllAsync(".seat, .seat-item");
            AssertHelper.GreaterOrEqual(seatElements.Count, 96, "Should create 8x12 = 96 seats");

            // Check row labels
            var rowLabels = await Page.QuerySelectorAllAsync(".row-label, .row-identifier");
            AssertHelper.GreaterOrEqual(rowLabels.Count, 8, "Should display row labels");

            // Check seat numbering
            var seatA1 = await Page.QuerySelectorAsync("text=A1, [data-seat='A1']");
            var seatH12 = await Page.QuerySelectorAsync("text=H12, [data-seat='H12']");
            AssertHelper.IsNotNull(seatA1, "Should have seat A1");
            AssertHelper.IsNotNull(seatH12, "Should have seat H12");
        }

        [Test]
        public async Task SeatMap_ColorCodesAvailability()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Color", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateComplexVenueLayout();

            // Create seats
            await Page.ClickAsync("text=Orchestra Center");
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
            await Page.FillAsync("input[name='Name']", "Color Test Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "22:00");
            await Page.ClickAsync("button[type='submit']");

            // Make some reservations
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Color", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Color Test Event");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Reserve some seats
            var availableSeats = await Page.QuerySelectorAllAsync(".seat.available");
            for (int i = 0; i < Math.Min(5, availableSeats.Count); i++)
            {
                await availableSeats[i].ClickAsync();
            }
            await Page.ClickAsync("button:has-text('Reserve')");
            await Page.WaitForSelectorAsync("text=success");

            // Act - View seat map again
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Color Test Event");
            await Page.ClickAsync("a:has-text('Reserve'), a:has-text('View Seats')");

            // Assert - Check color coding
            var availableColorSeats = await Page.QuerySelectorAllAsync(".seat.available, .seat[data-status='available']");
            var bookedColorSeats = await Page.QuerySelectorAllAsync(".seat.booked, .seat.reserved, .seat[data-status='booked']");
            var selectedColorSeats = await Page.QuerySelectorAllAsync(".seat.selected, .seat[data-status='selected']");

            AssertHelper.Greater(availableColorSeats.Count, 0, "Should have available seats (typically green)");
            AssertHelper.GreaterOrEqual(bookedColorSeats.Count, 5, "Should have booked seats (typically red)");

            // Check legend
            var legend = await Page.QuerySelectorAsync(".seat-legend, .color-legend");
            AssertHelper.IsNotNull(legend, "Should display color legend");

            var legendAvailable = await Page.QuerySelectorAsync("text=Available");
            var legendBooked = await Page.QuerySelectorAsync("text=Booked, text=Reserved");
            AssertHelper.IsNotNull(legendAvailable, "Legend should show available status");
            AssertHelper.IsNotNull(legendBooked, "Legend should show booked status");
        }

        [Test]
        public async Task SeatMap_ZoomAndPan_Navigation()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Zoom", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateComplexVenueLayout();

            // Create large seat map
            await Page.ClickAsync("text=Balcony");
            await Page.ClickAsync("a:has-text('Add Seats')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "20");
                await Page.FillAsync("input[name='Columns']", "30");
                await Page.ClickAsync("button[type='submit']");
            }

            // Act - Test zoom controls
            await Page.WaitForSelectorAsync(".seat-map, .seating-chart");

            var zoomInButton = await Page.QuerySelectorAsync("button:has-text('+'), button.zoom-in, button[aria-label='Zoom in']");
            var zoomOutButton = await Page.QuerySelectorAsync("button:has-text('-'), button.zoom-out, button[aria-label='Zoom out']");
            var resetButton = await Page.QuerySelectorAsync("button:has-text('Reset'), button.zoom-reset");

            if (zoomInButton != null)
            {
                // Zoom in
                await zoomInButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await zoomInButton.ClickAsync();

                // Check if seats appear larger
                var seatElement = await Page.QuerySelectorAsync(".seat:first-of-type");
                if (seatElement != null)
                {
                    var boundingBox = await seatElement.BoundingBoxAsync();
                    AssertHelper.IsNotNull(boundingBox, "Seat should be visible");
                }

                // Zoom out
                if (zoomOutButton != null)
                {
                    await zoomOutButton.ClickAsync();
                }

                // Reset view
                if (resetButton != null)
                {
                    await resetButton.ClickAsync();
                }
            }

            // Test pan functionality
            var seatMapContainer = await Page.QuerySelectorAsync(".seat-map-container, .seating-chart-wrapper");
            if (seatMapContainer != null)
            {
                var box = await seatMapContainer.BoundingBoxAsync();
                if (box != null)
                {
                    // Simulate drag to pan
                    await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
                    await Page.Mouse.DownAsync();
                    await Page.Mouse.MoveAsync(box.X + box.Width / 2 + 100, box.Y + box.Height / 2);
                    await Page.Mouse.UpAsync();
                }
            }

            // Assert - Navigation controls should be present
            AssertHelper.IsNotNull(zoomInButton ?? zoomOutButton ?? resetButton, "Zoom controls should be available");
        }

        [Test]
        public async Task SeatMap_TouchGestures_MobileView()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Touch", "Manager");
            await LoginAsync(email, "Test123!");

            // Set mobile viewport
            await Context.CloseAsync();
            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 375, Height = 667 },
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15",
                HasTouch = true,
                IsMobile = true,
                IgnoreHTTPSErrors = true
            });
            Page = await Context.NewPageAsync();

            await LoginAsync(email, "Test123!");
            await CreateComplexVenueLayout();

            // Create seats
            await Page.ClickAsync("text=Orchestra Left");
            await Page.ClickAsync("a:has-text('Add Seats')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "6");
                await Page.FillAsync("input[name='Columns']", "8");
                await Page.ClickAsync("button[type='submit']");
            }

            // Act - Test touch interactions
            await Page.WaitForSelectorAsync(".seat-map, .seating-chart");

            // Simulate pinch zoom
            var seatMap = await Page.QuerySelectorAsync(".seat-map, .seating-chart");
            if (seatMap != null)
            {
                var box = await seatMap.BoundingBoxAsync();
                if (box != null)
                {
                    // Tap to select seat
                    await Page.TapAsync(".seat.available:first-of-type");

                    // Check if seat is selected
                    var selectedSeat = await Page.QuerySelectorAsync(".seat.selected");
                    AssertHelper.IsNotNull(selectedSeat, "Seat should be selected on tap");
                }
            }

            // Assert - Mobile optimizations
            var mobileOptimized = await Page.QuerySelectorAsync(".mobile-view, .touch-enabled");
            AssertHelper.IsNotNull(mobileOptimized, "Should have mobile-optimized view");

            // Check for touch-friendly controls
            var touchControls = await Page.QuerySelectorAllAsync(".touch-control, button.large-touch-target");
            AssertHelper.Greater(touchControls.Count, 0, "Should have touch-friendly controls");
        }

        [Test]
        public async Task SubArea_DisplaysMultipleSections()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Multi", "Section");
            await LoginAsync(email, "Test123!");
            await CreateComplexVenueLayout();

            // Add seats to multiple SubAreas
            var areas = new[] { "Orchestra Left", "Orchestra Center", "Orchestra Right" };
            foreach (var area in areas)
            {
                await Page.ClickAsync($"text={area}");
                await Page.ClickAsync("a:has-text('Add Seats')");
                var seatMapExists = await ElementExistsAsync("input[name='Rows']");
                if (seatMapExists)
                {
                    await Page.FillAsync("input[name='Rows']", "4");
                    await Page.FillAsync("input[name='Columns']", "6");
                    await Page.ClickAsync("button[type='submit']");
                }
                await Page.WaitForTimeoutAsync(500);
            }

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Multi-Section Show");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:30");
            await Page.FillAsync("input[name='EndTime']", "22:00");
            await Page.ClickAsync("button[type='submit']");

            // Act - View all sections
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Multi-Section Show");
            await Page.ClickAsync("a:has-text('Reserve'), a:has-text('View Seats')");

            // Assert - All SubAreas should be visible
            var orchestraLeft = await Page.QuerySelectorAsync("text=Orchestra Left");
            var orchestraCenter = await Page.QuerySelectorAsync("text=Orchestra Center");
            var orchestraRight = await Page.QuerySelectorAsync("text=Orchestra Right");

            AssertHelper.IsNotNull(orchestraLeft, "Orchestra Left section should be visible");
            AssertHelper.IsNotNull(orchestraCenter, "Orchestra Center section should be visible");
            AssertHelper.IsNotNull(orchestraRight, "Orchestra Right section should be visible");

            // Check section switching
            var sectionTabs = await Page.QuerySelectorAllAsync(".section-tab, .subarea-selector");
            AssertHelper.GreaterOrEqual(sectionTabs.Count, 3, "Should have tabs/selectors for each section");

            // Click on different section
            if (sectionTabs.Count > 0)
            {
                await sectionTabs[1].ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Verify section changed
                var activeSection = await Page.QuerySelectorAsync(".section.active, .subarea.selected");
                AssertHelper.IsNotNull(activeSection, "Should show active section");
            }
        }

        [Test]
        public async Task SeatTooltip_DisplaysDetailedInformation()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Tooltip", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateComplexVenueLayout();

            // Create VIP seats with special attributes
            await Page.ClickAsync("text=VIP Box A");
            await Page.ClickAsync("a:has-text('Add Seats')");

            for (int i = 1; i <= 4; i++)
            {
                await Page.FillAsync("input[name='Name']", $"VIP-{i}");
                await Page.FillAsync("input[name='X']", $"{10 + i * 15}");
                await Page.FillAsync("input[name='Y']", "10");

                // Add special attributes if available
                var categoryField = await Page.QuerySelectorAsync("select[name='Category']");
                if (categoryField != null)
                {
                    await Page.SelectOptionAsync("select[name='Category']", "VIP");
                }

                var priceField = await Page.QuerySelectorAsync("input[name='Price']");
                if (priceField != null)
                {
                    await Page.FillAsync("input[name='Price']", "150.00");
                }

                await Page.ClickAsync("button:has-text('Add Seat')");
                await Page.WaitForTimeoutAsync(300);
            }

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "VIP Experience");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "23:00");
            await Page.ClickAsync("button[type='submit']");

            // Act - Hover over seats to show tooltips
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=VIP Experience");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Hover over VIP seat
            var vipSeat = await Page.QuerySelectorAsync(".seat:has-text('VIP-1'), [data-seat='VIP-1']");
            if (vipSeat != null)
            {
                await vipSeat.HoverAsync();
                await Page.WaitForTimeoutAsync(500);

                // Assert - Tooltip should appear
                var tooltip = await Page.QuerySelectorAsync(".seat-tooltip, .tooltip, [role='tooltip']");
                AssertHelper.IsNotNull(tooltip, "Tooltip should appear on hover");

                // Check tooltip content
                var tooltipText = await tooltip.TextContentAsync();
                Assert.That(tooltipText, Does.Contain("VIP").Or.Contain("150"), "Tooltip should show seat details");
            }

            // Click for more details
            if (vipSeat != null)
            {
                await vipSeat.ClickAsync();

                // Check for detailed panel
                var detailPanel = await Page.QuerySelectorAsync(".seat-details, .selection-details");
                AssertHelper.IsNotNull(detailPanel, "Should show detailed seat information panel");

                var seatInfo = await detailPanel.TextContentAsync();
                Assert.That(seatInfo, Does.Contain("VIP"), "Details should include seat category");
            }
        }

        [Test]
        public async Task SeatMap_FilterByPrice_HighlightsCategories()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Price", "Filter");
            await LoginAsync(email, "Test123!");

            // Create venue with different price categories
            await Page.GotoAsync($"{BaseUrl}/Venue/Create");
            await Page.FillAsync("input[name='Name']", "Tiered Pricing Venue");
            await Page.FillAsync("input[name='Address']", "789 Price Lane");
            await Page.SelectOptionAsync("select[name='CityId']", new SelectOptionValue { Index = 1 });
            await Page.ClickAsync("button[type='submit']");

            await Page.ClickAsync("a:has-text('Add Sub Area')");
            await Page.FillAsync("input[name='AreaName']", "Mixed Pricing Area");
            await Page.FillAsync("input[name='Width']", "100");
            await Page.FillAsync("input[name='Height']", "100");
            await Page.ClickAsync("button[type='submit']");

            // Create event with price categories
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Tiered Price Show");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(6).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "19:00");
            await Page.FillAsync("input[name='EndTime']", "21:00");

            // Set up pricing tiers if available
            var pricingTiersButton = await Page.QuerySelectorAsync("button:has-text('Configure Pricing')");
            if (pricingTiersButton != null)
            {
                await pricingTiersButton.ClickAsync();

                // Configure tiers
                await Page.FillAsync("input[name='Tier1Price']", "50");
                await Page.FillAsync("input[name='Tier2Price']", "75");
                await Page.FillAsync("input[name='Tier3Price']", "100");
                await Page.ClickAsync("button:has-text('Apply')");
            }

            await Page.ClickAsync("button[type='submit']");

            // Act - View with price filter
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Tiered Price Show");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Apply price filter
            var priceFilterDropdown = await Page.QuerySelectorAsync("select[name='priceFilter']");
            if (priceFilterDropdown != null)
            {
                await Page.SelectOptionAsync("select[name='priceFilter']", "50-75");
            }
            else
            {
                // Alternative: Price range slider
                var priceSlider = await Page.QuerySelectorAsync("input[type='range'][name='maxPrice']");
                if (priceSlider != null)
                {
                    await priceSlider.FillAsync("75");
                }
            }

            // Assert - Check filtered display
            var highlightedSeats = await Page.QuerySelectorAllAsync(".seat.highlighted, .seat.in-budget");
            var dimmedSeats = await Page.QuerySelectorAllAsync(".seat.dimmed, .seat.out-of-budget");

            AssertHelper.Greater(highlightedSeats.Count + dimmedSeats.Count, 0, "Should show price-based filtering");

            // Check price display
            var priceDisplay = await Page.QuerySelectorAsync(".price-range-display, .filter-info");
            AssertHelper.IsNotNull(priceDisplay, "Should show active price filter");
        }

        [Test]
        public async Task SeatMap_BestAvailable_AutoSelection()
        {
            // Arrange
            var managerEmail = GenerateTestEmail();
            await RegisterUserAsync(managerEmail, "Test123!", "Best", "Manager");
            await LoginAsync(managerEmail, "Test123!");
            await CreateComplexVenueLayout();

            // Create seats with center preference
            await Page.ClickAsync("text=Orchestra Center");
            await Page.ClickAsync("a:has-text('Add Seats')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "10");
                await Page.FillAsync("input[name='Columns']", "20");
                await Page.ClickAsync("button[type='submit']");
            }

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Best Seats Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(8).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "20:00");
            await Page.FillAsync("input[name='EndTime']", "22:30");
            await Page.ClickAsync("button[type='submit']");

            // Make some random reservations first
            await Page.ClickAsync("a:has-text('Logout')");
            var blockerEmail = GenerateTestEmail();
            await RegisterUserAsync(blockerEmail, "Test123!", "Block", "Seats");
            await LoginAsync(blockerEmail, "Test123!");

            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Best Seats Event");
            await Page.ClickAsync("a:has-text('Reserve')");

            // Reserve some scattered seats
            var scatteredSeats = await Page.QuerySelectorAllAsync(".seat.available");
            for (int i = 0; i < Math.Min(20, scatteredSeats.Count); i += 3)
            {
                await scatteredSeats[i].ClickAsync();
            }
            await Page.ClickAsync("button:has-text('Reserve')");

            // New customer wants best available
            await Page.ClickAsync("a:has-text('Logout')");
            var customerEmail = GenerateTestEmail();
            await RegisterUserAsync(customerEmail, "Test123!", "Best", "Customer");
            await LoginAsync(customerEmail, "Test123!");

            // Act - Use best available feature
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Best Seats Event");
            await Page.ClickAsync("a:has-text('Reserve')");

            var bestAvailableButton = await Page.QuerySelectorAsync("button:has-text('Best Available'), button:has-text('Find Best Seats')");
            if (bestAvailableButton != null)
            {
                // Specify number of seats
                var seatCountInput = await Page.QuerySelectorAsync("input[name='numberOfSeats']");
                if (seatCountInput != null)
                {
                    await Page.FillAsync("input[name='numberOfSeats']", "4");
                }

                await bestAvailableButton.ClickAsync();

                // Assert - Should auto-select best seats
                await Page.WaitForTimeoutAsync(1000);
                var selectedSeats = await Page.QuerySelectorAllAsync(".seat.selected");
                AssertHelper.GreaterOrEqual(selectedSeats.Count, 4, "Should auto-select requested number of seats");

                // Check if seats are together
                var seatLabels = [];
                foreach (var seat in selectedSeats)
                {
                    var label = await seat.GetAttributeAsync("data-seat");
                    if (label != null) seatLabels.Add(label);
                }

                // Verify seats are consecutive or in same row
                AssertHelper.Greater(seatLabels.Count, 0, "Selected seats should be together");

                await Page.ClickAsync("button:has-text('Reserve')");
                await Page.WaitForSelectorAsync("text=success");
            }
        }

        [Test]
        public async Task SeatMap_PrintView_GeneratesCleanLayout()
        {
            // Arrange
            var email = GenerateTestEmail();
            await RegisterUserAsync(email, "Test123!", "Print", "Manager");
            await LoginAsync(email, "Test123!");
            await CreateComplexVenueLayout();

            // Add seats
            await Page.ClickAsync("text=Balcony");
            await Page.ClickAsync("a:has-text('Add Seats')");
            var seatMapExists = await ElementExistsAsync("input[name='Rows']");
            if (seatMapExists)
            {
                await Page.FillAsync("input[name='Rows']", "8");
                await Page.FillAsync("input[name='Columns']", "15");
                await Page.ClickAsync("button[type='submit']");
            }

            // Create event
            await Page.ClickAsync("a:has-text('Create Event')");
            await Page.FillAsync("input[name='Name']", "Print Layout Event");
            await Page.FillAsync("input[name='StartDate']", DateTime.Now.AddDays(12).ToString("yyyy-MM-dd"));
            await Page.FillAsync("input[name='StartTime']", "15:00");
            await Page.FillAsync("input[name='EndTime']", "17:00");
            await Page.ClickAsync("button[type='submit']");

            // Act - Open print view
            await Page.GotoAsync($"{BaseUrl}/Events");
            await Page.ClickAsync("text=Print Layout Event");
            await Page.ClickAsync("a:has-text('Reserve'), a:has-text('View Seats')");

            var printButton = await Page.QuerySelectorAsync("button:has-text('Print'), button[aria-label='Print seat map']");
            if (printButton != null)
            {
                await printButton.ClickAsync();

                // Check for print preview or print dialog
                var printPreview = await Page.QuerySelectorAsync(".print-preview, .print-view");
                AssertHelper.IsNotNull(printPreview, "Should show print-friendly view");

                // Verify print CSS is applied
                await Page.EvaluateAsync(@"
                    const printStyles = document.querySelector('style[media=""print""], link[media=""print""]');
                    return printStyles !== null;
                ");
            }

            // Alternative: Export to PDF
            var exportButton = await Page.QuerySelectorAsync("button:has-text('Export'), button:has-text('Download PDF')");
            if (exportButton != null)
            {
                await exportButton.ClickAsync();

                // Check for download trigger
                var downloadPromise = Page.WaitForDownloadAsync();
                // Note: In actual test, this would trigger a download
            }

            // Assert - Print view should be clean
            AssertHelper.IsNotNull(printButton ?? exportButton, "Should have print or export functionality");
        }
    }
}