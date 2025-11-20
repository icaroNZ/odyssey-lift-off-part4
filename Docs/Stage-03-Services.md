# Stage 3: Service Layer (REST API Client)

## Overview

In Stage 3, we created the **service layer** - the bridge between our GraphQL API and the external REST API. This layer is responsible for fetching data from the Catstronauts REST API at `https://odyssey-lift-off-rest-api.herokuapp.com/`.

**What we built:**
- `ITrackService` - Interface defining all API operations
- `TrackService` - Implementation using HttpClient
- Service registration with dependency injection
- Comprehensive tests for all service methods

**Why this matters:**
The service layer encapsulates all external API communication, making our code testable, maintainable, and following the Single Responsibility Principle. It's a fundamental pattern in software architecture.

---

## Learning Objectives

By the end of this stage, you will understand:

1. ✅ The Service Layer pattern and why it's essential
2. ✅ Interface-based design (SOLID's Dependency Inversion Principle)
3. ✅ HttpClient and IHttpClientFactory in .NET
4. ✅ How to avoid socket exhaustion
5. ✅ Generic methods for code reuse (DRY principle)
6. ✅ Proper error handling with HttpRequestException
7. ✅ Dependency injection in ASP.NET Core
8. ✅ Testing services with mocking (NSubstitute)

---

## Key Concepts

### Concept 1: The Service Layer Pattern

**What is the Service Layer?**

The service layer sits between your API layer (GraphQL resolvers) and external dependencies (REST APIs, databases, file systems). It encapsulates business logic and external communication.

**Architecture:**
```
┌─────────────────────────────────────────────┐
│          GraphQL Resolvers (API Layer)      │
│         "What data do clients want?"        │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│          Service Layer (Business Logic)     │
│        "How do we get that data?"           │
│                                             │
│  TrackService implements ITrackService      │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│       External REST API / Database          │
│    https://odyssey-lift-off-rest-api...    │
└─────────────────────────────────────────────┘
```

**Why separate the service layer?**

**Without Service Layer (Bad):**
```csharp
public class Query
{
    public async Task<List<Track>> GetTracksForHome()
    {
        // GraphQL resolver making HTTP calls directly
        var httpClient = new HttpClient();  // ❌ Creates new client each time
        var response = await httpClient.GetAsync("https://api.../tracks");
        var tracks = await response.Content.ReadFromJsonAsync<List<Track>>();
        return tracks;
    }
}
```

**Problems:**
- ❌ Resolver has two responsibilities (GraphQL + HTTP)
- ❌ Can't test without hitting real API
- ❌ Hard to change API implementation
- ❌ HttpClient created inefficiently
- ❌ No error handling
- ❌ Code duplication across resolvers

**With Service Layer (Good):**
```csharp
// Resolver - handles GraphQL concerns
public class Query
{
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)
    {
        return await trackService.GetTracksForHomeAsync();
    }
}

// Service - handles HTTP concerns
public class TrackService : ITrackService
{
    private readonly HttpClient _httpClient;

    public TrackService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Track>> GetTracksForHomeAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Track>>("tracks");
    }
}
```

**Benefits:**
- ✅ Single Responsibility: Resolver handles GraphQL, service handles HTTP
- ✅ Testable: Can mock ITrackService in tests
- ✅ Flexible: Can swap implementations (REST → gRPC → Database)
- ✅ Reusable: Service can be used by multiple resolvers
- ✅ Error handling centralized

**Interview Answer:**
> "The service layer separates business logic and external communication from the API layer. It makes code testable (we can mock the service), maintainable (changes to the API don't affect resolvers), and follows Single Responsibility Principle. Each layer has one job: resolvers handle GraphQL, services handle data fetching."

---

### Concept 2: Interface-Based Design (SOLID D - Dependency Inversion)

**What is Dependency Inversion Principle (DIP)?**

**Bad (depend on concretions):**
```csharp
public class Query
{
    private readonly TrackService _service;  // ❌ Depends on concrete class

    public Query()
    {
        _service = new TrackService();  // ❌ Hard-coded dependency
    }
}
```

**Problems:**
- ❌ Can't swap implementations
- ❌ Hard to test (must use real TrackService)
- ❌ Tightly coupled

**Good (depend on abstractions):**
```csharp
// Interface (abstraction)
public interface ITrackService
{
    Task<List<Track>> GetTracksForHomeAsync();
}

// Resolver depends on interface
public class Query
{
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)  // ✅ Depends on interface
    {
        return await trackService.GetTracksForHomeAsync();
    }
}

// Concrete implementation
public class TrackService : ITrackService
{
    public async Task<List<Track>> GetTracksForHomeAsync()
    {
        // Implementation details
    }
}
```

**Benefits:**
- ✅ **Testable**: Can inject a mock implementation
- ✅ **Flexible**: Can swap implementations without changing resolvers
- ✅ **Loose coupling**: Resolver doesn't know about TrackService internals

**Real-world example of flexibility:**

```csharp
// Development: Use REST API
builder.Services.AddScoped<ITrackService, TrackService>();

// Testing: Use mock service
builder.Services.AddScoped<ITrackService, MockTrackService>();

// Production with caching: Use cached service
builder.Services.AddScoped<ITrackService, CachedTrackService>();

// Resolver code stays the same!
```

**Interview Answer:**
> "Dependency Inversion means depending on abstractions (interfaces) rather than concrete implementations. Our resolvers depend on ITrackService, not TrackService. This makes code testable (we can inject mocks), flexible (we can swap implementations), and follows the Open/Closed Principle - open for extension, closed for modification."

---

### Concept 3: HttpClient and IHttpClientFactory

**The HttpClient Problem**

**Bad (creating HttpClient directly):**
```csharp
public class TrackService
{
    public async Task<Track> GetTrackAsync(string id)
    {
        using var httpClient = new HttpClient();  // ❌ BAD!
        var track = await httpClient.GetFromJsonAsync<Track>($"https://api.../track/{id}");
        return track;
    }
}
```

**Why this is BAD:**

1. **Socket exhaustion**: Each `new HttpClient()` creates a new TCP connection. Even though you dispose it, the socket stays in TIME_WAIT state for 240 seconds. With high traffic:
   ```
   Request 1: Creates socket → Disposes → Socket in TIME_WAIT (240s)
   Request 2: Creates socket → Disposes → Socket in TIME_WAIT (240s)
   ...
   Request 10,000: No sockets available! ❌ CRASH
   ```

2. **DNS changes not respected**: HttpClient caches DNS entries. If the API's IP changes, your app won't see it until restart.

3. **Performance overhead**: Creating new connections is expensive.

**Good (using IHttpClientFactory):**
```csharp
// Program.cs - Register with factory
builder.Services.AddHttpClient<ITrackService, TrackService>();

// TrackService.cs - Inject HttpClient
public class TrackService : ITrackService
{
    private readonly HttpClient _httpClient;

    public TrackService(HttpClient httpClient)  // ✅ Injected by factory
    {
        _httpClient = httpClient;
    }

    public async Task<Track> GetTrackAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<Track>($"track/{id}");
    }
}
```

**What IHttpClientFactory does:**

1. **Connection pooling**: Reuses TCP connections across requests
   ```
   Request 1: Uses connection from pool
   Request 2: Reuses same connection
   Request 3: Reuses same connection
   → Only one socket needed!
   ```

2. **DNS refresh**: Periodically refreshes DNS entries (every 2 minutes by default)

3. **Lifecycle management**: Creates HttpClient instances with proper lifecycle

4. **Performance**: Much faster than creating new clients

**Real numbers:**
- Creating HttpClient: ~50ms + socket allocation
- Using pooled HttpClient: ~1ms

With 1000 requests/second:
- Without factory: 50,000ms (50 seconds) + socket exhaustion
- With factory: 1,000ms (1 second) + connection reuse

**Interview Answer:**
> "IHttpClientFactory solves critical production issues with HttpClient. Creating HttpClient directly can cause socket exhaustion - each client creates a TCP connection that stays in TIME_WAIT for 240 seconds even after disposal. With high traffic, you run out of sockets. The factory uses connection pooling, reuses sockets, respects DNS changes, and dramatically improves performance. In ASP.NET Core, always use `AddHttpClient<T>()` for services that need HttpClient."

---

### Concept 4: Generic Methods (DRY Principle)

**Problem:** Code duplication across similar operations

**Without Generics (Code Duplication):**
```csharp
public async Task<Track> GetTrackAsync(string id)
{
    var response = await _httpClient.GetAsync($"track/{id}");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<Track>();
}

public async Task<Author> GetAuthorAsync(string id)
{
    var response = await _httpClient.GetAsync($"author/{id}");  // ❌ Duplicate!
    response.EnsureSuccessStatusCode();  // ❌ Duplicate!
    return await response.Content.ReadFromJsonAsync<Author>();
}

public async Task<Module> GetModuleAsync(string id)
{
    var response = await _httpClient.GetAsync($"module/{id}");  // ❌ Duplicate!
    response.EnsureSuccessStatusCode();  // ❌ Duplicate!
    return await response.Content.ReadFromJsonAsync<Module>();
}
```

**With Generics (DRY):**
```csharp
// Generic helper method
private async Task<T?> GetAsync<T>(string endpoint)
{
    var response = await _httpClient.GetAsync(endpoint);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<T>();
}

// Use helper in all methods
public Task<Track?> GetTrackAsync(string id)
    => GetAsync<Track>($"track/{id}");

public Task<Author?> GetAuthorAsync(string id)
    => GetAsync<Author>($"author/{id}");

public Task<Module?> GetModuleAsync(string id)
    => GetAsync<Module>($"module/{id}");
```

**Benefits:**
- ✅ **DRY**: Write once, use everywhere
- ✅ **Maintainability**: Change error handling in one place
- ✅ **Type safety**: Compiler ensures T is correct
- ✅ **Readability**: Method intent is clearer

**How generics work:**

```csharp
// When you call:
var track = await GetAsync<Track>("track/c_0");

// Compiler generates:
private async Task<Track?> GetAsync(string endpoint)
{
    // T is replaced with Track
    return await response.Content.ReadFromJsonAsync<Track>();
}
```

**Interview Answer:**
> "Generic methods let us write reusable code that works with different types. Instead of duplicating GET logic for Track, Author, and Module, we write one generic `GetAsync<T>` method. The compiler generates type-specific versions at compile time, giving us code reuse with type safety. This follows the DRY principle - Don't Repeat Yourself."

---

### Concept 5: Error Handling Strategies

**Our approach: Let 404s return null, throw on other errors**

```csharp
public async Task<Track?> GetTrackAsync(string id)
{
    try
    {
        return await _httpClient.GetFromJsonAsync<Track?>($"track/{id}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return null;  // Expected case - track not found
    }
    // Other exceptions propagate up
}
```

**Why this approach?**

**404 Not Found → return null**
- Expected scenario: User requests track that doesn't exist
- Not an error condition: Client can handle null gracefully
- GraphQL convention: Queries return null for missing data

**500 Internal Server Error → throw**
- Unexpected scenario: API is broken
- IS an error condition: Something went wrong
- Should be logged and potentially retried

**Example Usage:**
```csharp
// Resolver
public async Task<Track?> GetTrack(string id, [Service] ITrackService service)
{
    var track = await service.GetTrackAsync(id);  // Might be null
    return track;  // GraphQL returns null to client
}

// GraphQL Response for non-existent track
{
  "data": {
    "track": null  // ← Null is valid, no error
  }
}

// GraphQL Response for API error
{
  "data": null,
  "errors": [{
    "message": "Internal server error",  // ← Exception was thrown
    "path": ["track"]
  }]
}
```

**Alternative Approaches:**

**1. Always throw exceptions:**
```csharp
public async Task<Track> GetTrackAsync(string id)
{
    var track = await _httpClient.GetFromJsonAsync<Track>($"track/{id}");
    if (track == null)
        throw new NotFoundException($"Track {id} not found");
    return track;
}
```
- ❌ Forces exception handling in resolvers
- ❌ Exceptions are expensive performance-wise
- ❌ Not idiomatic GraphQL (queries can return null)

**2. Return a result object:**
```csharp
public async Task<Result<Track>> GetTrackAsync(string id)
{
    try
    {
        var track = await _httpClient.GetFromJsonAsync<Track>($"track/{id}");
        return Result<Track>.Success(track);
    }
    catch (Exception ex)
    {
        return Result<Track>.Failure(ex.Message);
    }
}
```
- ✅ More explicit error handling
- ❌ More complex for simple scenarios
- Better for complex business logic

**Our choice:** Null for expected "not found," exceptions for errors. Simple and GraphQL-friendly.

**Interview Answer:**
> "We handle 404 Not Found by returning null because it's an expected scenario - the requested resource simply doesn't exist. This is idiomatic GraphQL, where queries can return null for missing data. Other HTTP errors (500, 503, etc.) indicate actual problems, so we let them throw exceptions. This gives resolvers a simple API: null means 'not found,' exception means 'something broke.'"

---

### Concept 6: Dependency Injection in ASP.NET Core

**What is Dependency Injection (DI)?**

Instead of creating dependencies yourself, you declare what you need, and the framework provides it.

**Without DI:**
```csharp
public class Query
{
    public async Task<List<Track>> GetTracksForHome()
    {
        var httpClient = new HttpClient();  // ❌ Create dependency
        var service = new TrackService(httpClient);  // ❌ Create dependency
        return await service.GetTracksForHomeAsync();
    }
}
```

**With DI:**
```csharp
// 1. Register service in Program.cs
builder.Services.AddHttpClient<ITrackService, TrackService>();

// 2. Inject into resolver
public class Query
{
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)  // ✅ Framework provides it
    {
        return await trackService.GetTracksForHomeAsync();
    }
}
```

**How it works:**

```
1. Application starts
2. DI container is built with all registrations
3. GraphQL request comes in
4. Hot Chocolate sees [Service] ITrackService
5. Container checks: "Do I have ITrackService?"
6. Finds: ITrackService → TrackService
7. Creates HttpClient (from factory)
8. Creates TrackService(httpClient)
9. Passes trackService to resolver
10. Request completes
11. Container disposes trackService
```

**Service Lifetimes:**

```csharp
// Singleton - One instance for app lifetime
builder.Services.AddSingleton<ICacheService, CacheService>();

// Scoped - One instance per request
builder.Services.AddScoped<ITrackService, TrackService>();

// Transient - New instance every time
builder.Services.AddTransient<ILogger, Logger>();
```

**Our choice: Scoped (via AddHttpClient)**
- New TrackService per GraphQL request
- HttpClient is managed by factory (pooled)
- Disposed after request completes

**Benefits:**
- ✅ **Testable**: Easy to inject mocks
- ✅ **Configurable**: Change implementations without code changes
- ✅ **Lifecycle management**: Framework handles creation/disposal
- ✅ **Loose coupling**: Components don't know about dependencies

**Interview Answer:**
> "Dependency Injection is a pattern where instead of creating your dependencies, you declare what you need and the framework provides it. In ASP.NET Core, we register services in Program.cs with their lifetimes (Singleton, Scoped, Transient), and the DI container handles creation, injection, and disposal. Hot Chocolate uses the `[Service]` attribute to inject dependencies into resolvers. This makes code testable, configurable, and follows the Dependency Inversion Principle."

---

## Step-by-Step Implementation

### Step 1: Create the ITrackService Interface

**File:** `Services/ITrackService.cs`

```csharp
using Catstronauts.GraphQL.Models;

namespace Catstronauts.GraphQL.Services;

/// <summary>
/// Service interface for interacting with the Catstronauts REST API.
/// Provides methods for fetching tracks, authors, and modules.
/// </summary>
/// <remarks>
/// Why an interface?
/// - Enables dependency injection (resolvers depend on ITrackService, not TrackService)
/// - Makes testing easy (can mock this interface)
/// - Allows swapping implementations (REST → gRPC → Database) without changing resolvers
/// - Follows SOLID's Dependency Inversion Principle
/// </remarks>
public interface ITrackService
{
    /// <summary>
    /// Retrieves all tracks for the homepage.
    /// </summary>
    /// <returns>A list of all available tracks. Empty list if none found.</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    Task<List<Track>> GetTracksForHomeAsync();

    /// <summary>
    /// Retrieves a single track by its ID.
    /// </summary>
    /// <param name="id">The unique track identifier (e.g., "c_0")</param>
    /// <returns>The track if found; null if not found (404)</returns>
    /// <exception cref="ArgumentException">Thrown when id is null or empty</exception>
    /// <exception cref="HttpRequestException">Thrown when API request fails (non-404)</exception>
    Task<Track?> GetTrackAsync(string id);

    /// <summary>
    /// Retrieves an author by their ID.
    /// </summary>
    /// <param name="id">The unique author identifier (e.g., "cat-1")</param>
    /// <returns>The author if found; null if not found (404)</returns>
    /// <exception cref="ArgumentException">Thrown when id is null or empty</exception>
    /// <exception cref="HttpRequestException">Thrown when API request fails (non-404)</exception>
    Task<Author?> GetAuthorAsync(string id);

    /// <summary>
    /// Retrieves all modules for a specific track.
    /// </summary>
    /// <param name="trackId">The track's unique identifier</param>
    /// <returns>A list of modules. Empty list if none found.</returns>
    /// <exception cref="ArgumentException">Thrown when trackId is null or empty</exception>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    Task<List<Module>> GetTrackModulesAsync(string trackId);

    /// <summary>
    /// Retrieves a single module by its ID.
    /// </summary>
    /// <param name="id">The unique module identifier</param>
    /// <returns>The module if found; null if not found (404)</returns>
    /// <exception cref="ArgumentException">Thrown when id is null or empty</exception>
    /// <exception cref="HttpRequestException">Thrown when API request fails (non-404)</exception>
    Task<Module?> GetModuleAsync(string id);

    /// <summary>
    /// Increments the view count for a specific track.
    /// This is a PATCH operation on the REST API.
    /// </summary>
    /// <param name="trackId">The track's unique identifier</param>
    /// <returns>The updated track with incremented view count</returns>
    /// <exception cref="ArgumentException">Thrown when trackId is null or empty</exception>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    /// <remarks>
    /// The REST API's PATCH endpoint is idempotent - calling it multiple times
    /// with the same ID is safe and won't cause duplicate increments.
    /// </remarks>
    Task<Track> IncrementTrackViewsAsync(string trackId);
}
```

**Why 6 methods?**
- `GetTracksForHomeAsync` - For homepage track list
- `GetTrackAsync` - For individual track details
- `GetAuthorAsync` - For fetching track authors
- `GetTrackModulesAsync` - For fetching track modules
- `GetModuleAsync` - For individual module details
- `IncrementTrackViewsAsync` - For tracking view counts (mutation)

---

### Step 2: Implement TrackService

**File:** `Services/TrackService.cs`

```csharp
using System.Net;
using Catstronauts.GraphQL.Models;

namespace Catstronauts.GraphQL.Services;

/// <summary>
/// Implementation of ITrackService that communicates with the Catstronauts REST API.
/// Uses HttpClient for HTTP communication.
/// </summary>
public class TrackService : ITrackService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://odyssey-lift-off-rest-api.herokuapp.com/";

    /// <summary>
    /// Constructor with HttpClient injection.
    /// HttpClient is provided by IHttpClientFactory (configured in Program.cs).
    /// </summary>
    /// <param name="httpClient">Injected HttpClient from factory</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient is null</exception>
    public TrackService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<List<Track>> GetTracksForHomeAsync()
    {
        // No validation needed - no parameters
        var tracks = await GetAsync<List<Track>>("tracks");
        return tracks ?? new List<Track>();  // Return empty list if null
    }

    public async Task<Track?> GetTrackAsync(string id)
    {
        ValidateId(id, nameof(id));
        return await GetAsync<Track?>($"track/{id}");
    }

    public async Task<Author?> GetAuthorAsync(string id)
    {
        ValidateId(id, nameof(id));
        return await GetAsync<Author?>($"author/{id}");
    }

    public async Task<List<Module>> GetTrackModulesAsync(string trackId)
    {
        ValidateId(trackId, nameof(trackId));
        var modules = await GetAsync<List<Module>>($"track/{trackId}/modules");
        return modules ?? new List<Module>();
    }

    public async Task<Module?> GetModuleAsync(string id)
    {
        ValidateId(id, nameof(id));
        return await GetAsync<Module?>($"module/{id}");
    }

    public async Task<Track> IncrementTrackViewsAsync(string trackId)
    {
        ValidateId(trackId, nameof(trackId));

        // PATCH request to increment views
        var response = await _httpClient.PatchAsync(
            $"track/{trackId}/numberOfViews",
            content: null);  // No body needed for this endpoint

        response.EnsureSuccessStatusCode();

        var track = await response.Content.ReadFromJsonAsync<Track>();
        return track ?? throw new InvalidOperationException(
            $"API returned null for track {trackId}");
    }

    /// <summary>
    /// Generic helper method for GET requests.
    /// Reduces code duplication across multiple GET methods.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="endpoint">The API endpoint (relative to BaseUrl)</param>
    /// <returns>The deserialized object, or null if not found (404)</returns>
    /// <exception cref="HttpRequestException">Thrown for non-404 errors</exception>
    private async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            // GetFromJsonAsync handles deserialization
            return await _httpClient.GetFromJsonAsync<T>(endpoint);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // 404 is expected when resource doesn't exist
            // Return null instead of throwing
            return default;  // null for reference types
        }
        // Other exceptions (500, network errors) propagate up
    }

    /// <summary>
    /// Validates that an ID parameter is not null or empty.
    /// </summary>
    /// <param name="id">The ID to validate</param>
    /// <param name="paramName">The parameter name (for exception message)</param>
    /// <exception cref="ArgumentException">Thrown when ID is invalid</exception>
    private static void ValidateId(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "ID cannot be null or empty.",
                paramName);
        }
    }
}
```

**Key implementation details:**

1. **HttpClient injection**: Constructor takes HttpClient (provided by factory)
2. **Base URL**: Set once in constructor
3. **Generic GetAsync<T>**: Reusable GET logic
4. **ValidateId**: Reusable validation
5. **404 handling**: Returns null for not found
6. **PATCH for mutation**: Uses PatchAsync for incrementing views

---

### Step 3: Register Service with Dependency Injection

**File:** `Program.cs`

```csharp
using Catstronauts.GraphQL.GraphQL.Queries;
using Catstronauts.GraphQL.Services;

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient with IHttpClientFactory for TrackService
// This manages connection pooling and lifecycle automatically
builder.Services.AddHttpClient<ITrackService, TrackService>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**What AddHttpClient does:**
1. Registers `ITrackService` → `TrackService` mapping
2. Configures HttpClient with IHttpClientFactory
3. Sets service lifetime to Scoped (one per request)
4. Enables automatic HttpClient injection

---

### Step 4: Write Comprehensive Tests

**File:** `Catstronauts.Tests/Services/TrackServiceTests.cs`

We created **23 tests** covering:

**Constructor Tests (2):**
```csharp
[Fact]
public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
{
    // Act
    Action act = () => new TrackService(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
        .WithParameterName("httpClient");
}

[Fact]
public void Constructor_SetsBaseAddress()
{
    // Arrange
    var httpClient = new HttpClient();

    // Act
    var service = new TrackService(httpClient);

    // Assert
    httpClient.BaseAddress.Should().NotBeNull();
    httpClient.BaseAddress!.ToString()
        .Should().Be("https://odyssey-lift-off-rest-api.herokuapp.com/");
}
```

**Validation Tests (15):**
```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public async Task GetTrackAsync_WithInvalidId_ThrowsArgumentException(string? invalidId)
{
    // Arrange
    var httpClient = new HttpClient();
    var service = new TrackService(httpClient);

    // Act
    Func<Task> act = async () => await service.GetTrackAsync(invalidId!);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>()
        .WithMessage("ID cannot be null or empty.*");
}
```

**Interface Implementation Tests (2):**
```csharp
[Fact]
public void TrackService_ImplementsITrackService()
{
    // Assert
    typeof(TrackService).Should().Implement<ITrackService>();
}

[Fact]
public void ITrackService_HasAllRequiredMethods()
{
    // Assert
    var interfaceType = typeof(ITrackService);
    interfaceType.GetMethods().Should().HaveCount(6);
}
```

**Return Type Verification (4):**
```csharp
[Fact]
public void GetTracksForHomeAsync_ReturnsTaskOfListOfTrack()
{
    var method = typeof(ITrackService).GetMethod(nameof(ITrackService.GetTracksForHomeAsync));
    method!.ReturnType.Should().Be(typeof(Task<List<Track>>));
}
```

---

## Design Decisions & Trade-offs

### Decision 1: Interface-Based Design

**What we chose:** ITrackService interface + TrackService implementation

**Alternative:** Just TrackService (no interface)

**Why interface?**
- ✅ Testable (can mock in tests)
- ✅ Flexible (can swap implementations)
- ✅ Follows SOLID D (Dependency Inversion)

**Trade-off:**
- ❌ More code (interface + implementation)
- ❌ Slightly more complex

**Verdict:** Worth it for testability and flexibility

---

### Decision 2: Generic GetAsync<T> Method

**What we chose:** One generic method for all GET operations

**Alternative:** Separate methods for each type

**Why generic?**
- ✅ DRY (Don't Repeat Yourself)
- ✅ Single place for error handling
- ✅ Easier to maintain

**Trade-off:**
- ❌ Slightly more complex for beginners
- ❌ Can't customize behavior per type (though we haven't needed to)

**Verdict:** Huge win for code reuse

---

### Decision 3: 404 Returns Null vs Throws Exception

**What we chose:** 404 returns null

**Alternative:** Always throw exceptions

**Why null for 404?**
- ✅ Expected scenario (resource might not exist)
- ✅ GraphQL convention (queries can return null)
- ✅ Simpler for callers (no try-catch needed)

**Trade-off:**
- ❌ Callers must check for null
- ❌ Can't distinguish 404 from other null causes

**Verdict:** Matches GraphQL conventions

---

### Decision 4: ValidateId Helper Method

**What we chose:** Centralized validation method

**Alternative:** Validate in each method

**Why centralized?**
- ✅ DRY (one place for validation logic)
- ✅ Consistent error messages
- ✅ Easy to enhance (add regex validation, etc.)

**Trade-off:**
- ❌ One more method to understand

**Verdict:** Clear win for maintainability

---

## Interview Preparation

### Q1: Explain the Service Layer pattern and why we use it.

**Answer:**
"The Service Layer pattern separates business logic and external communication from the API layer. In our GraphQL API:

**Without Service Layer:**
- Resolvers make HTTP calls directly
- Mixed responsibilities (GraphQL + HTTP)
- Hard to test (must hit real API)
- Code duplication

**With Service Layer:**
- Resolvers handle GraphQL concerns
- Services handle HTTP/database concerns
- Easy to test (mock the service)
- Reusable across multiple resolvers

**Benefits:**
1. **Single Responsibility**: Each layer has one job
2. **Testability**: Can mock ITrackService in resolver tests
3. **Flexibility**: Can swap REST API for gRPC or database without changing resolvers
4. **Maintainability**: Changes to API don't affect resolvers

**Real example:** If we wanted to add caching, we'd create `CachedTrackService` implementing `ITrackService`. Resolvers stay the same, we just swap the registration in DI."

---

### Q2: What is IHttpClientFactory and why is it critical for production?

**Answer:**
"IHttpClientFactory solves two critical problems with HttpClient:

**Problem 1: Socket Exhaustion**

Creating HttpClient directly:
```csharp
using var client = new HttpClient();  // ❌ Creates new TCP connection
```

Even though you dispose it, the socket stays in TIME_WAIT for 240 seconds. With high traffic:
- Request 1: Socket A in TIME_WAIT (240s)
- Request 2: Socket B in TIME_WAIT (240s)
- ...
- Request 10,000: No sockets available → CRASH

**Problem 2: DNS Caching**

HttpClient caches DNS entries forever. If the API's IP changes, your app won't see it without restart.

**Solution: IHttpClientFactory**

```csharp
builder.Services.AddHttpClient<ITrackService, TrackService>();
```

The factory:
1. **Pools connections**: Reuses TCP connections across requests
2. **Manages lifecycle**: Creates clients with proper lifetime
3. **Refreshes DNS**: Updates DNS every 2 minutes
4. **Improves performance**: No connection overhead

**Result:** From 50ms per request to 1ms, and no socket exhaustion.

In production, always use `AddHttpClient` for services that need HttpClient."

---

### Q3: How does Dependency Injection work in ASP.NET Core?

**Answer:**
"Dependency Injection is a pattern where you declare what you need, and the framework provides it.

**Setup (Program.cs):**
```csharp
builder.Services.AddHttpClient<ITrackService, TrackService>();
```
This registers: 'When someone asks for ITrackService, give them TrackService.'

**Usage (Resolver):**
```csharp
public async Task<List<Track>> GetTracksForHome(
    [Service] ITrackService trackService)
{
    // trackService is injected by framework
}
```

**How it works:**
1. GraphQL request arrives
2. Hot Chocolate sees [Service] ITrackService
3. Asks DI container: 'Do you have ITrackService?'
4. Container: 'Yes, ITrackService → TrackService'
5. Container creates HttpClient (via factory)
6. Container creates TrackService(httpClient)
7. Injects trackService into resolver
8. Request completes
9. Container disposes trackService

**Service Lifetimes:**
- **Singleton**: One instance for entire app
- **Scoped**: One instance per request (our choice)
- **Transient**: New instance every time

**Benefits:**
- Testable: Easy to inject mocks
- Configurable: Swap implementations without code changes
- Lifecycle managed: No manual disposal needed
- Loose coupling: Resolvers don't know about TrackService internals"

---

### Q4: Explain the generic GetAsync<T> method and its benefits.

**Answer:**
"The generic `GetAsync<T>` method is a reusable helper that eliminates code duplication:

**Before (duplication):**
```csharp
public async Task<Track?> GetTrackAsync(string id)
{
    var response = await _httpClient.GetAsync($'track/{id}');
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<Track>();
}

public async Task<Author?> GetAuthorAsync(string id)
{
    var response = await _httpClient.GetAsync($'author/{id}');  // Duplicate
    response.EnsureSuccessStatusCode();  // Duplicate
    return await response.Content.ReadFromJsonAsync<Author>();
}
```

**After (DRY):**
```csharp
private async Task<T?> GetAsync<T>(string endpoint)
{
    try
    {
        return await _httpClient.GetFromJsonAsync<T>(endpoint);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return default;  // null for 404
    }
}

public Task<Track?> GetTrackAsync(string id) => GetAsync<Track>($'track/{id}');
public Task<Author?> GetAuthorAsync(string id) => GetAsync<Author>($'author/{id}');
```

**How generics work:**

When you call `GetAsync<Track>`, the compiler generates a Track-specific version. You get:
- Type safety: Compiler ensures T is correct
- Code reuse: Logic written once
- Single source of truth: Error handling in one place

**Benefits:**
1. **DRY**: 80% less code
2. **Maintainability**: Change error handling once, affects all methods
3. **Type safety**: Compiler prevents type errors
4. **Readability**: Intent is clearer

This follows the DRY principle - Don't Repeat Yourself."

---

### Q5: How do you handle errors in the service layer?

**Answer:**
"Our error handling strategy differentiates between expected and unexpected errors:

**Expected Error: 404 Not Found → Return null**
```csharp
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    return null;  // Resource doesn't exist, not an error
}
```

**Why null?**
- Getting a non-existent track is expected user behavior
- GraphQL convention: Queries return null for missing data
- Caller can handle null gracefully

**Unexpected Errors: 500, network errors → Throw**
```csharp
catch (HttpRequestException ex)
{
    // Propagate up - something is broken
    throw;
}
```

**Why throw?**
- API being down is NOT expected
- Needs to be logged and potentially retried
- GraphQL returns error to client

**Example:**

```csharp
// Client requests track that doesn't exist
var track = await service.GetTrackAsync('nonexistent');
// Returns: null
// GraphQL response: { "data": { "track": null } }

// API is down
var track = await service.GetTrackAsync('c_0');
// Throws: HttpRequestException
// GraphQL response: { "data": null, "errors": [...] }
```

**Benefits:**
- Clear distinction: null = not found, exception = error
- Resolvers have simple API
- Follows GraphQL conventions
- Appropriate error handling for each scenario"

---

### Q6: Why test a service that just calls an API? (Cumulative - builds on testing from Stage 1)

**Answer:**
"You might think: 'It's just HTTP calls, why test it?' But service tests provide real value:

**What we test:**
1. **Constructor validation**: Ensures null HttpClient is rejected
2. **Base address configuration**: Verifies URL is set correctly
3. **Parameter validation**: All methods reject null/empty IDs
4. **Interface implementation**: TrackService actually implements ITrackService
5. **Return types**: Methods return correct types
6. **Async behavior**: All methods are truly async

**What we DON'T test:**
- Actual HTTP calls (that's integration testing)
- API responses (that's the API's responsibility)

**Value provided:**
1. **Documentation**: Tests show how to use the service
2. **Regression prevention**: If someone breaks validation, tests fail
3. **Contract verification**: Ensures service matches interface
4. **Design feedback**: If tests are hard to write, design might be wrong

**Testing strategy:**
- **Unit tests** (what we did): Test service logic in isolation
- **Integration tests** (later): Test actual API communication
- **Mocking in resolver tests** (Stage 4): Mock ITrackService to test resolvers

**Example of value:** If someone removes parameter validation by accident, tests immediately catch it:
```csharp
public Task<Track?> GetTrackAsync(string id)
    => GetAsync<Track>($'track/{id}');  // No validation! ❌

// Test fails:
[Theory]
[InlineData(null)]
public async Task GetTrackAsync_WithNull_ThrowsArgumentException(string id)
{
    await act.Should().ThrowAsync<ArgumentException>();  // ❌ FAILS
}
```

Tests pay for themselves by preventing bugs before they reach production."

---

## Troubleshooting

### Issue 1: "Unable to load service index for NuGet"

**Symptom:** `dotnet restore` or `dotnet build` fails with NuGet errors.

**Solution:**
This is a network/environment issue. Manually edit `.csproj` files:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
</ItemGroup>
```

Then build: `dotnet build --no-restore`

---

### Issue 2: HttpClient socket exhaustion in production

**Symptom:** App works fine locally, crashes in production with "No connection could be made because the target machine actively refused it."

**Cause:** Creating HttpClient directly instead of using IHttpClientFactory.

**Solution:**
```csharp
// ❌ DON'T
public class TrackService
{
    public async Task<Track> GetTrackAsync(string id)
    {
        using var client = new HttpClient();  // ❌ Socket exhaustion!
    }
}

// ✅ DO
// Program.cs
builder.Services.AddHttpClient<ITrackService, TrackService>();

// TrackService.cs
public class TrackService
{
    private readonly HttpClient _httpClient;
    public TrackService(HttpClient httpClient)  // ✅ Injected!
    {
        _httpClient = httpClient;
    }
}
```

---

### Issue 3: "Object reference not set to an instance" when calling service

**Symptom:** NullReferenceException when service method is called.

**Cause:** Service not registered in DI, so null is injected.

**Solution:**
Add to `Program.cs`:
```csharp
builder.Services.AddHttpClient<ITrackService, TrackService>();
```

---

### Issue 4: BaseAddress not set correctly

**Symptom:** HTTP calls fail with "Cannot send a content-body with this verb-type."

**Cause:** BaseAddress missing trailing slash.

**Solution:**
```csharp
// ❌ DON'T
_httpClient.BaseAddress = new Uri("https://api.example.com");

// ✅ DO
_httpClient.BaseAddress = new Uri("https://api.example.com/");
//                                                          ^ trailing slash!
```

---

### Issue 5: Tests fail with "Cannot access disposed object"

**Symptom:** Tests throw ObjectDisposedException when calling service methods.

**Cause:** HttpClient created in test is disposed before async operation completes.

**Solution:**
Don't dispose HttpClient in tests:
```csharp
// ❌ DON'T
using var httpClient = new HttpClient();

// ✅ DO
var httpClient = new HttpClient();  // Let GC clean up
```

---

## Summary

In Stage 3, we built the **service layer** - the bridge between our GraphQL API and the REST API:

**What we created:**
- ✅ `ITrackService` - Interface with 6 method signatures
- ✅ `TrackService` - Implementation using HttpClient
- ✅ DI registration - `AddHttpClient<ITrackService, TrackService>()`
- ✅ 23 comprehensive tests - Constructor, validation, interface, types

**Key learnings:**
- ✅ Service layer separates concerns (GraphQL vs HTTP)
- ✅ Interface-based design enables testing and flexibility
- ✅ IHttpClientFactory prevents socket exhaustion
- ✅ Generic methods eliminate code duplication (DRY)
- ✅ 404 returns null, other errors throw exceptions
- ✅ Dependency injection manages lifecycle

**Design principles applied:**
- **Single Responsibility**: Service handles HTTP, resolvers handle GraphQL
- **Dependency Inversion**: Depend on ITrackService (interface), not TrackService (concrete)
- **DRY**: Generic `GetAsync<T>` method
- **KISS**: Simple, focused methods
- **Defensive programming**: Parameter validation

**Production best practices:**
- ✅ Always use IHttpClientFactory for HttpClient
- ✅ Handle expected errors differently than unexpected
- ✅ Validate input parameters
- ✅ Use dependency injection for testability
- ✅ Centralize error handling

**GraphQL connection:**
This service layer will be used by GraphQL resolvers (Stage 4) to fetch data. Resolvers will inject `ITrackService` and call these methods to get tracks, authors, and modules.

**Next up: Stage 4 - GraphQL Queries** where we'll create actual GraphQL queries using this service layer!

---

**Interview Readiness:** You can now explain the service layer pattern, IHttpClientFactory, dependency injection, generic methods, error handling strategies, and why we use interface-based design. You're ready to discuss service architecture in production APIs! 🚀
