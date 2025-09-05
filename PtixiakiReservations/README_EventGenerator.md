# 🎭 Event Generator

A comprehensive system for generating realistic test data including venues, sub-areas, seats, and events.

## Features

### 🏛️ **Venue Generation**
- Creates realistic venues with Greek names and locations
- Includes addresses with real Greek street names
- Assigns venues to Greek cities (Athens, Thessaloniki, Patras, etc.)
- Generates phone numbers and postal codes
- Adds venue images from Unsplash

### 📍 **Sub-Area Generation**
- Creates multiple sub-areas per venue (Orchestra, Balcony, VIP, etc.)
- Realistic dimensions and positioning
- Proper descriptions for each area type

### 🪑 **Seat Generation**  
- Intelligent seat layout based on sub-area dimensions
- Row-based naming (A01, A02, B01, etc.)
- 90% availability rate (realistic occupancy)
- Proper positioning within sub-areas

### 🎪 **Event Generation**
- Future-dated events only (1-90 days ahead)
- Realistic event names and types
- Concert, theater, comedy, musical events
- Proper time scheduling (10 AM - 11 PM)
- 1-4 hour event durations

## Usage

### Via Web Interface
1. Navigate to `/EventGenerator` in your browser
2. Configure generation options:
   - Number of venues (1-20)
   - Sub-areas per venue (2-5)
   - Events per venue (3-8)
   - Seat generation (20-100 per sub-area)
   - Date range (1-365 days ahead)
3. Click "🚀 Generate Events"
4. View comprehensive results and statistics

### Via Code
```csharp
// Inject the service
private readonly IEventGeneratorService _eventGenerator;

// Configure options
var options = new EventGenerationOptions
{
    VenueCount = 3,
    MinSubAreasPerVenue = 2,
    MaxSubAreasPerVenue = 4,
    MinEventsPerVenue = 5,
    MaxEventsPerVenue = 10,
    GenerateSeats = true,
    MinSeatsPerSubArea = 30,
    MaxSeatsPerSubArea = 150,
    MinDaysInFuture = 1,
    MaxDaysInFuture = 60
};

// Generate data
var result = await _eventGenerator.GenerateEventsAsync(options);

if (result.Success)
{
    Console.WriteLine($"Generated {result.TotalItemsGenerated} items:");
    Console.WriteLine($"- {result.GeneratedVenues.Count} venues");
    Console.WriteLine($"- {result.GeneratedSubAreas.Count} sub-areas");
    Console.WriteLine($"- {result.GeneratedSeats.Count} seats");  
    Console.WriteLine($"- {result.GeneratedEvents.Count} events");
}
```

## Configuration Options

| Option | Description | Default | Range |
|--------|-------------|---------|-------|
| `VenueCount` | Number of venues to create | 5 | 1-20 |
| `MinSubAreasPerVenue` | Minimum sub-areas per venue | 2 | 1-10 |
| `MaxSubAreasPerVenue` | Maximum sub-areas per venue | 5 | 1-10 |
| `MinEventsPerVenue` | Minimum events per venue | 3 | 1-20 |
| `MaxEventsPerVenue` | Maximum events per venue | 8 | 1-20 |
| `GenerateSeats` | Whether to generate seats | true | true/false |
| `MinSeatsPerSubArea` | Minimum seats per sub-area | 20 | 5-500 |
| `MaxSeatsPerSubArea` | Maximum seats per sub-area | 100 | 5-500 |
| `MinDaysInFuture` | Minimum days ahead for events | 1 | 1-365 |
| `MaxDaysInFuture` | Maximum days ahead for events | 90 | 1-365 |

## Generated Data Examples

### Venues
- "Grand Theater" in Athens
- "Royal Concert Hall" in Thessaloniki  
- "Metropolitan Arena" in Patras

### Events
- "Rock Concert - Grand Theater" (2025-01-15 20:00-23:30)
- "Classical Symphony - Royal Opera House" (2025-01-20 19:00-21:00)
- "Comedy Show - Central Club" (2025-01-25 21:00-22:30)

### Sub-Areas
- Orchestra (200x150 capacity ~300 seats)
- Balcony (180x100 capacity ~150 seats)
- VIP Section (100x80 capacity ~50 seats)

## Cleanup

The system provides easy cleanup of all generated data:

### Via Web Interface
- Click "🗑️ Cleanup Generated Data" button
- Confirms before deletion

### Via Code  
```csharp
var success = await _eventGenerator.CleanupGeneratedDataAsync();
```

## Implementation Details

### Service Registration
```csharp
// Program.cs
builder.Services.AddScoped<IEventGeneratorService, EventGeneratorService>();
```

### Database Impact
- Creates test user: `event-generator-user`
- Ensures cities and event types exist
- All generated data is tagged for easy cleanup
- Uses proper Entity Framework relationships

### Safety Features
- All events are future-dated only
- User isolation (generated content tied to test user)
- Easy cleanup prevents data pollution
- Input validation on all parameters

## Navigation

Access the Event Generator at: `/EventGenerator`

The web interface provides:
- 📊 Real-time generation preview
- 🎯 Success/error feedback  
- 📋 Detailed generation results
- 🔄 Easy regeneration options
- 🗑️ One-click cleanup

## Perfect for:
- ✅ Development testing
- ✅ Demo environments  
- ✅ Performance testing
- ✅ UI/UX testing
- ✅ Integration testing
- ✅ Training environments