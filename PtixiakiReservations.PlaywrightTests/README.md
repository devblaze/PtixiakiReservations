# Playwright UI Tests for PtixiakiReservations

This project contains end-to-end UI tests using Playwright for automated browser testing.

## Prerequisites

1. .NET 9.0 SDK
2. Node.js (for Playwright browsers)
3. The main application running locally

## Setup

### 1. Install Dependencies

```bash
# Restore NuGet packages
dotnet restore

# Install Playwright browsers
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install

# Or on Mac/Linux:
./bin/Debug/net9.0/playwright.sh install
```

### 2. Configure Test Environment

Update the `BaseUrl` in `TestBase.cs` to match your local development URL:
```csharp
protected string BaseUrl = "http://localhost:5000"; // Your app URL
```

### 3. Start the Application

Before running tests, ensure your application is running:
```bash
cd ../PtixiakiReservations
dotnet run
```

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Category
```bash
# Run only Venue Management tests
dotnet test --filter "FullyQualifiedName~VenueManagementTests"

# Run only Event Management tests
dotnet test --filter "FullyQualifiedName~EventManagementTests"

# Run only Reservation tests
dotnet test --filter "FullyQualifiedName~ReservationTests"
```

### Run Specific Test
```bash
dotnet test --filter "CreateVenueWithSubAreaAndSeats_CompleteWorkflow"
```

### Run Tests with Visible Browser (Debugging)
Edit `TestBase.cs` and set `Headless = false`:
```csharp
Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false, // See the browser
    SlowMo = 500 // Slow down actions for debugging
});
```

### Run Tests with Different Browsers
```bash
# Run with Chromium (default)
dotnet test

# Run with Firefox
dotnet test --filter "Browser=Firefox"

# Run with WebKit (Safari)
dotnet test --filter "Browser=WebKit"
```

## Test Categories

### 1. Venue Management Tests (`VenueManagementTests.cs`)
- **CreateVenueWithSubAreaAndSeats_CompleteWorkflow**: Creates a venue with sub-areas and seats
- **EditVenue_UpdatesInformation**: Tests venue editing functionality
- **ViewMyVenues_DisplaysVenueList**: Verifies venue listing
- **DeleteVenue_RemovesVenue**: Tests venue deletion
- **CreateMultipleSubAreas_Success**: Tests creating multiple sub-areas

### 2. Event Management Tests (`EventManagementTests.cs`)
- **CreateEvent_SingleDay_Success**: Creates a single-day event
- **CreateEvent_MultipleDays_CreatesIndividualEvents**: Creates events spanning multiple days
- **EditEvent_UpdatesDetails**: Tests event editing
- **ViewVenueEvents_DisplaysAllEvents**: Verifies event listing
- **CreateRecurringEvents_Weekly**: Tests recurring event creation
- **DeleteEvent_RemovesFromList**: Tests event deletion

### 3. Reservation Tests (`ReservationTests.cs`)
- **SearchEvents_ByName_FindsCorrectEvents**: Tests event search by name
- **SearchEvents_ByCity_FiltersCorrectly**: Tests event filtering by city
- **ReserveSeats_CompleteWorkflow**: Complete reservation process
- **ViewMyReservations_DisplaysAllReservations**: Tests reservation listing
- **CancelReservation_FreesUpSeats**: Tests reservation cancellation
- **PreventDoubleBooking_SameSeat**: Verifies double-booking prevention
- **EventsForToday_ShowsCurrentEvents**: Tests today's events display

## Test Output

### Screenshots
Failed tests automatically capture screenshots in the `screenshots/` folder.

### Videos
Test execution videos are saved in the `videos/` folder (when enabled).

### Reports
- HTML Report: `playwright-report/index.html`
- JSON Report: `test-results.json`

## Continuous Integration

### GitHub Actions Example
```yaml
name: Playwright Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2

    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '9.0.x'

    - name: Install dependencies
      run: |
        dotnet restore
        dotnet build
        pwsh bin/Debug/net9.0/playwright.ps1 install

    - name: Run Application
      run: |
        cd PtixiakiReservations
        dotnet run &
        sleep 10  # Wait for app to start

    - name: Run Playwright tests
      run: |
        cd PtixiakiReservations.PlaywrightTests
        dotnet test

    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v2
      with:
        name: playwright-report
        path: PtixiakiReservations.PlaywrightTests/playwright-report
```

## Troubleshooting

### Tests fail with "Connection refused"
- Ensure the application is running on the correct port
- Check the `BaseUrl` in `TestBase.cs`

### Browser not installed
Run the Playwright install command:
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

### Tests are too fast/slow
Adjust the `SlowMo` value in `TestBase.cs`:
```csharp
SlowMo = 100 // Milliseconds delay between actions
```

### Need more debugging information
Set `Headless = false` and increase `SlowMo` to see what's happening.

## Best Practices

1. **Keep tests independent**: Each test should be able to run in isolation
2. **Use unique test data**: Generate unique emails/names to avoid conflicts
3. **Clean up after tests**: Delete test data when possible
4. **Use proper waits**: Use `WaitForSelectorAsync` instead of hard-coded delays
5. **Take screenshots**: Capture screenshots on failures for debugging
6. **Test multiple browsers**: Run tests on Chromium, Firefox, and WebKit
7. **Use Page Object Model**: For larger test suites, consider implementing Page Object Model pattern

## License

See main project license.