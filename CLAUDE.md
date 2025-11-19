# Development Best Practices & Standards

## 🎯 Mission Statement

This document defines the standards, principles, and practices we will follow while migrating the Catstronauts application from Node.js to C# with Hot Chocolate GraphQL. These guidelines ensure code quality, maintainability, and successful learning outcomes.

---

## 📋 Core Principles

### 1. SOLID Principles

#### **S - Single Responsibility Principle (SRP)**
*"A class should have one, and only one, reason to change."*

**✅ DO:**
```csharp
// Good: Each class has one responsibility
public class TrackService : ITrackService
{
    // Only responsible for API communication
    public async Task<Track> GetTrackAsync(string id) { }
}

public class TrackValidator
{
    // Only responsible for validation
    public bool ValidateTrackId(string id) { }
}
```

**❌ DON'T:**
```csharp
// Bad: Class doing too many things
public class TrackService
{
    public async Task<Track> GetTrackAsync(string id) { }
    public bool ValidateTrackId(string id) { }  // Mixing concerns
    public void LogTrackAccess(string id) { }   // Mixing concerns
    public string FormatTrackTitle(string title) { }  // Mixing concerns
}
```

**Why it matters:** Single responsibility makes code easier to test, maintain, and understand. When requirements change, you know exactly which class to modify.

---

#### **O - Open/Closed Principle (OCP)**
*"Software entities should be open for extension, but closed for modification."*

**✅ DO:**
```csharp
// Good: Use interfaces and inheritance
public interface ITrackService
{
    Task<List<Track>> GetTracksForHomeAsync();
}

// Can extend without modifying existing code
public class CachedTrackService : ITrackService
{
    private readonly ITrackService _innerService;
    private readonly IMemoryCache _cache;

    public async Task<List<Track>> GetTracksForHomeAsync()
    {
        // Add caching behavior without changing original
        return await _cache.GetOrCreateAsync("tracks",
            entry => _innerService.GetTracksForHomeAsync());
    }
}
```

**❌ DON'T:**
```csharp
// Bad: Hard to extend without modification
public class TrackService
{
    public async Task<List<Track>> GetTracksForHomeAsync(bool useCache)
    {
        if (useCache) { /* caching logic */ }
        else { /* normal logic */ }
        // Must modify this method to add new behaviors
    }
}
```

**Why it matters:** Extension without modification prevents bugs in existing functionality and allows for flexible, composable designs.

---

#### **L - Liskov Substitution Principle (LSP)**
*"Derived classes must be substitutable for their base classes."*

**✅ DO:**
```csharp
// Good: Substitutable implementations
public interface ITrackService
{
    Task<Track?> GetTrackAsync(string id);
}

public class RestTrackService : ITrackService
{
    public async Task<Track?> GetTrackAsync(string id)
    {
        // Returns null if not found - consistent with interface contract
        return await FetchFromApiAsync(id);
    }
}

public class MockTrackService : ITrackService
{
    public async Task<Track?> GetTrackAsync(string id)
    {
        // Also returns null if not found - same behavior
        return _testData.FirstOrDefault(t => t.Id == id);
    }
}
```

**❌ DON'T:**
```csharp
// Bad: Violates expected behavior
public class ThrowingTrackService : ITrackService
{
    public async Task<Track?> GetTrackAsync(string id)
    {
        // Throws exception instead of returning null - violates LSP!
        var track = await FetchFromApiAsync(id);
        if (track == null)
            throw new NotFoundException($"Track {id} not found");
        return track;
    }
}
```

**Why it matters:** Substitutability ensures predictable behavior and prevents surprises when swapping implementations.

---

#### **I - Interface Segregation Principle (ISP)**
*"Many client-specific interfaces are better than one general-purpose interface."*

**✅ DO:**
```csharp
// Good: Focused interfaces
public interface ITrackReader
{
    Task<Track> GetTrackAsync(string id);
    Task<List<Track>> GetTracksForHomeAsync();
}

public interface ITrackWriter
{
    Task<Track> IncrementViewsAsync(string id);
}

// Clients only depend on what they need
public class Query
{
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackReader trackReader)  // Only needs reading
    {
        return await trackReader.GetTracksForHomeAsync();
    }
}
```

**❌ DON'T:**
```csharp
// Bad: Fat interface
public interface ITrackService
{
    Task<Track> GetTrackAsync(string id);
    Task<List<Track>> GetTracksForHomeAsync();
    Task<Track> IncrementViewsAsync(string id);
    Task<Track> CreateTrackAsync(Track track);
    Task DeleteTrackAsync(string id);
    Task<byte[]> ExportTracksAsync();
    Task ImportTracksAsync(byte[] data);
    // Forces clients to depend on methods they don't use
}
```

**Why it matters:** Smaller interfaces reduce coupling and make testing easier (fewer methods to mock).

---

#### **D - Dependency Inversion Principle (DIP)**
*"Depend upon abstractions, not concretions."*

**✅ DO:**
```csharp
// Good: Depend on interface
public class Query
{
    private readonly ITrackService _trackService;  // Interface

    public Query(ITrackService trackService)
    {
        _trackService = trackService;
    }

    public async Task<List<Track>> GetTracksForHome()
    {
        return await _trackService.GetTracksForHomeAsync();
    }
}

// Easy to swap implementations
builder.Services.AddScoped<ITrackService, RestTrackService>();
// or
builder.Services.AddScoped<ITrackService, MockTrackService>();
```

**❌ DON'T:**
```csharp
// Bad: Depend on concrete class
public class Query
{
    private readonly TrackService _trackService;  // Concrete class

    public Query()
    {
        _trackService = new TrackService();  // Hard-coded dependency
    }

    // Impossible to test or swap implementations
}
```

**Why it matters:** Abstraction enables testing, flexibility, and loose coupling. You can swap implementations without changing client code.

---

### 2. KISS Principle (Keep It Simple, Stupid)

**✅ DO:**
```csharp
// Simple and clear
public bool IsValidTrackId(string id)
{
    return !string.IsNullOrWhiteSpace(id);
}
```

**❌ DON'T:**
```csharp
// Over-engineered
public bool IsValidTrackId(string id)
{
    var validator = new TrackIdValidatorFactory()
        .CreateValidator(ValidatorType.Standard)
        .WithRule(new NotNullRule())
        .WithRule(new NotEmptyRule())
        .WithRule(new NotWhitespaceRule())
        .Build();
    return validator.Validate(id).IsValid;
}
```

**Guidelines:**
- Start with the simplest solution that works
- Add complexity only when needed
- Prefer readability over cleverness
- Avoid premature optimization
- Question every abstraction layer

---

### 3. DRY Principle (Don't Repeat Yourself)

**✅ DO:**
```csharp
// Reusable method
public class TrackService
{
    private async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public Task<Track> GetTrackAsync(string id)
        => GetAsync<Track>($"track/{id}");

    public Task<Module> GetModuleAsync(string id)
        => GetAsync<Module>($"module/{id}");
}
```

**❌ DON'T:**
```csharp
// Repeated logic
public class TrackService
{
    public async Task<Track> GetTrackAsync(string id)
    {
        var response = await _httpClient.GetAsync($"track/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Track>();
    }

    public async Task<Module> GetModuleAsync(string id)
    {
        var response = await _httpClient.GetAsync($"module/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Module>();
    }
}
```

**Guidelines:**
- Extract common patterns into methods
- Use inheritance/composition for shared behavior
- Configuration over code duplication
- Balance: Don't abstract prematurely (YAGNI - You Aren't Gonna Need It)

---

## 🧪 Testing Standards

### Mandatory Testing Requirements

**⚠️ CRITICAL RULE: ALL TESTS MUST PASS BEFORE COMPLETING A TASK**

Every feature must include:
1. ✅ Unit tests for business logic
2. ✅ Integration tests for GraphQL endpoints
3. ✅ All tests must pass (green)
4. ✅ Aim for 80%+ code coverage

### Testing Principles

#### 1. AAA Pattern (Arrange-Act-Assert)

```csharp
[Fact]
public async Task GetTrack_WithValidId_ReturnsTrack()
{
    // Arrange: Set up test data and dependencies
    var mockService = Substitute.For<ITrackService>();
    mockService.GetTrackAsync("t_01")
        .Returns(new Track { Id = "t_01", Title = "Test Track" });
    var query = new Query();

    // Act: Execute the code under test
    var result = await query.GetTrack("t_01", mockService);

    // Assert: Verify the results
    result.Should().NotBeNull();
    result.Id.Should().Be("t_01");
    result.Title.Should().Be("Test Track");
}
```

#### 2. Test Naming Convention

Format: `MethodName_Scenario_ExpectedBehavior`

**✅ Good Names:**
- `GetTrack_WithValidId_ReturnsTrack`
- `GetTrack_WithNullId_ThrowsArgumentException`
- `IncrementViews_WhenApiSucceeds_ReturnsSuccessResponse`
- `IncrementViews_WhenApiFails_ReturnsErrorResponse`

**❌ Bad Names:**
- `Test1`
- `TestGetTrack`
- `ItWorks`

#### 3. Test Coverage Requirements

**Must test:**
- ✅ Happy path (normal scenarios)
- ✅ Edge cases (nulls, empty strings, boundary values)
- ✅ Error scenarios (exceptions, API failures)
- ✅ Async operations complete correctly

**Example:**
```csharp
public class TrackServiceTests
{
    [Fact]
    public async Task GetTrack_WithValidId_ReturnsTrack() { }

    [Fact]
    public async Task GetTrack_WithNullId_ThrowsArgumentException() { }

    [Fact]
    public async Task GetTrack_WhenApiReturns404_ReturnsNull() { }

    [Fact]
    public async Task GetTrack_WhenApiThrowsException_PropagatesException() { }
}
```

#### 4. Testing Tools

- **xUnit**: Test framework (modern, extensible)
- **NSubstitute**: Mocking library (simple, fluent API)
- **FluentAssertions**: Assertion library (readable, expressive)

**Example with all three:**
```csharp
[Fact]
public async Task IncrementTrackViews_Success_ReturnsCorrectResponse()
{
    // Arrange - NSubstitute mock
    var mockService = Substitute.For<ITrackService>();
    mockService.IncrementViewsAsync("t_01")
        .Returns(new Track { Id = "t_01", NumberOfViews = 101 });

    var mutation = new Mutation();

    // Act
    var response = await mutation.IncrementTrackViews("t_01", mockService);

    // Assert - FluentAssertions
    response.Should().NotBeNull();
    response.Success.Should().BeTrue();
    response.Code.Should().Be(200);
    response.Track.Should().NotBeNull();
    response.Track!.NumberOfViews.Should().Be(101);
}
```

#### 5. Integration Testing

Test full GraphQL requests:
```csharp
[Fact]
public async Task Query_TracksForHome_ReturnsValidData()
{
    // Arrange
    var query = @"
        query {
            tracksForHome {
                id
                title
                author {
                    name
                }
            }
        }
    ";

    // Act
    var result = await ExecuteRequestAsync(query);

    // Assert
    result.Errors.Should().BeNullOrEmpty();
    result.Data.Should().NotBeNull();
    var tracks = result.Data["tracksForHome"];
    tracks.Should().NotBeNull();
}
```

---

## 💻 Code Quality Standards

### 1. Naming Conventions

**C# Standard Conventions:**
- **PascalCase**: Classes, methods, properties, public fields
  - `public class TrackService`
  - `public async Task<Track> GetTrackAsync(string id)`

- **camelCase**: Private fields, parameters, local variables
  - `private readonly ITrackService _trackService;` (with underscore prefix for fields)
  - `public void DoSomething(string trackId)`
  - `var localVariable = 123;`

- **UPPER_CASE**: Constants
  - `private const int MAX_RETRIES = 3;`

**Meaningful Names:**
```csharp
// ✅ Good
public async Task<List<Track>> GetTracksForHomeAsync()
public async Task<Track> IncrementTrackViewsAsync(string trackId)

// ❌ Bad
public async Task<List<Track>> GetData()
public async Task<Track> DoStuff(string id)
```

### 2. Async/Await Best Practices

**✅ DO:**
```csharp
// All I/O operations should be async
public async Task<Track> GetTrackAsync(string id)
{
    var response = await _httpClient.GetAsync($"track/{id}");
    return await response.Content.ReadFromJsonAsync<Track>();
}

// Use async suffix for async methods
public async Task<List<Track>> GetTracksForHomeAsync() { }
```

**❌ DON'T:**
```csharp
// Don't block on async code
public Track GetTrack(string id)
{
    return _httpClient.GetAsync($"track/{id}").Result;  // BAD: Blocking
}

// Don't use async void (except event handlers)
public async void GetTrack(string id) { }  // BAD: Can't catch exceptions
```

### 3. Error Handling

**✅ DO:**
```csharp
// Specific exceptions
public async Task<Track> GetTrackAsync(string id)
{
    if (string.IsNullOrWhiteSpace(id))
        throw new ArgumentException("Track ID cannot be null or empty", nameof(id));

    try
    {
        return await _httpClient.GetFromJsonAsync<Track>($"track/{id}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return null;  // Expected case: track not found
    }
    // Let other exceptions propagate
}
```

**❌ DON'T:**
```csharp
// Swallowing exceptions
public async Task<Track> GetTrackAsync(string id)
{
    try
    {
        return await _httpClient.GetFromJsonAsync<Track>($"track/{id}");
    }
    catch (Exception)
    {
        return null;  // BAD: Hides all errors, even unexpected ones
    }
}
```

### 4. Null Handling (C# 11+ Nullable Reference Types)

**✅ Enable nullable reference types:**
```xml
<!-- In .csproj -->
<Nullable>enable</Nullable>
```

```csharp
// Explicit nullability
public async Task<Track?> GetTrackAsync(string id)  // ? means can return null
{
    // Compiler enforces null checks
}

// Non-nullable
public async Task<List<Track>> GetTracksForHomeAsync()  // Never null
{
    return tracks ?? new List<Track>();  // Guarantee non-null
}
```

### 5. Method Size & Complexity

**Guidelines:**
- Methods should fit on one screen (~20-30 lines max)
- If longer, extract helper methods
- Cyclomatic complexity < 10

**✅ DO:**
```csharp
public async Task<IncrementTrackViewsResponse> IncrementTrackViews(
    string id,
    [Service] ITrackService trackService)
{
    try
    {
        var track = await trackService.IncrementViewsAsync(id);
        return CreateSuccessResponse(track, id);
    }
    catch (HttpRequestException ex)
    {
        return CreateErrorResponse(ex);
    }
}

private IncrementTrackViewsResponse CreateSuccessResponse(Track track, string id)
{
    return new IncrementTrackViewsResponse
    {
        Code = 200,
        Success = true,
        Message = $"Successfully incremented views for track {id}",
        Track = track
    };
}

private IncrementTrackViewsResponse CreateErrorResponse(HttpRequestException ex)
{
    return new IncrementTrackViewsResponse
    {
        Code = (int)(ex.StatusCode ?? HttpStatusCode.InternalServerError),
        Success = false,
        Message = ex.Message,
        Track = null
    };
}
```

### 6. Comments & Documentation

**When to comment:**
- **Why**, not **what**: Explain reasoning, not mechanics
- Complex business logic
- Non-obvious workarounds
- Public APIs (XML doc comments)

**✅ Good comments:**
```csharp
/// <summary>
/// Increments the view count for a track. Note: This is an idempotent
/// operation - calling multiple times with same ID is safe.
/// </summary>
/// <param name="id">The unique identifier of the track</param>
/// <returns>Updated track with new view count</returns>
public async Task<Track> IncrementViewsAsync(string id)
{
    // PATCH endpoint is idempotent per API contract
    return await PatchAsync<Track>($"track/{id}/numberOfViews");
}
```

**❌ Bad comments:**
```csharp
// Get track by ID
public async Task<Track> GetTrackAsync(string id)  // Comment adds no value
{
    // Call the API
    var response = await _httpClient.GetAsync($"track/{id}");  // Obvious
    // Return the track
    return await response.Content.ReadFromJsonAsync<Track>();  // Obvious
}
```

### 7. Dependency Injection

**✅ DO:**
```csharp
// Register in Program.cs
builder.Services.AddScoped<ITrackService, TrackService>();
builder.Services.AddHttpClient<TrackService>();

// Inject via constructor
public class TrackService : ITrackService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrackService> _logger;

    public TrackService(HttpClient httpClient, ILogger<TrackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
}

// Inject into resolvers
[Query]
public async Task<List<Track>> GetTracksForHome(
    [Service] ITrackService trackService)
{
    return await trackService.GetTracksForHomeAsync();
}
```

---

## 🔄 Development Workflow

### 1. Before Starting Any Task

**MANDATORY: Ask Clarifying Questions**

Before writing code, ensure you understand:
- ✅ What is the expected behavior?
- ✅ What are the edge cases?
- ✅ What should happen on errors?
- ✅ How will this be tested?
- ✅ Are there any performance considerations?

**Example Questions:**
- "Should `GetTrack` return null or throw an exception if the track isn't found?"
- "What HTTP status codes should we handle from the API?"
- "Should we cache the results? For how long?"

### 2. Task Completion Checklist

Before marking a task as complete:
- ✅ Code compiles without warnings
- ✅ All tests pass (green)
- ✅ Code follows SOLID principles
- ✅ Code is readable and well-named
- ✅ Error handling is appropriate
- ✅ Async/await used correctly
- ✅ No code duplication
- ✅ Documentation added (if public API)

### 3. Git Commit Messages

**Format:** `<type>: <subject>`

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `test`: Adding tests
- `refactor`: Code improvement without changing behavior
- `docs`: Documentation changes
- `chore`: Maintenance tasks

**Examples:**
```
feat: Add TrackService with REST API integration
test: Add unit tests for Query resolver
refactor: Extract error handling into helper methods
docs: Add XML comments to ITrackService interface
```

---

## 📊 Performance Considerations

### 1. Async All the Way

```csharp
// ✅ Async throughout the stack
public async Task<List<Track>> GetTracksForHomeAsync()
{
    return await _httpClient.GetFromJsonAsync<List<Track>>("tracks");
}
```

### 2. Use DataLoaders for N+1 Prevention

```csharp
// Without DataLoader: N+1 queries (bad)
// Query tracks → For each track, query author (N queries)

// With DataLoader: 2 queries (good)
// Query tracks → Batch all author queries into one
[GraphQLName("author")]
public async Task<Author> GetAuthorAsync(
    [Parent] Track track,
    [Service] AuthorDataLoader authorLoader)
{
    return await authorLoader.LoadAsync(track.AuthorId);
}
```

### 3. HTTP Client Best Practices

```csharp
// ✅ Use IHttpClientFactory (manages connection pooling)
builder.Services.AddHttpClient<ITrackService, TrackService>();

// ❌ Don't create HttpClient directly
// var client = new HttpClient();  // BAD: Socket exhaustion
```

---

## 🎓 Learning Mindset

### Growth Principles

1. **Mistakes are learning opportunities**
   - Bugs teach us edge cases
   - Failed tests show us what we missed
   - Code reviews reveal better patterns

2. **Question everything (respectfully)**
   - "Why do we use interfaces here?"
   - "Could we simplify this?"
   - "What happens if this fails?"

3. **Incremental progress**
   - Small, working steps > big, broken leaps
   - Green tests > perfect code
   - Refactor working code, don't write perfect code first

4. **Document your learning**
   - Take notes on new concepts
   - Comment your "aha!" moments
   - Ask questions when stuck

---

## ✅ Summary Checklist

Before starting any task:
- [ ] Understand the requirement fully
- [ ] Ask clarifying questions
- [ ] Review relevant best practices in this document

While working:
- [ ] Follow SOLID principles
- [ ] Keep it simple (KISS)
- [ ] Don't repeat yourself (DRY)
- [ ] Write tests as you go

Before completing a task:
- [ ] All tests pass
- [ ] Code is clean and readable
- [ ] Error handling is appropriate
- [ ] Follows naming conventions
- [ ] Async/await used correctly
- [ ] No compiler warnings

---

## 📚 Additional Resources

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Hot Chocolate Documentation](https://chillicream.com/docs/hotchocolate/v13)
- [xUnit Documentation](https://xunit.net/)
- [Clean Code by Robert C. Martin](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-19
**Author:** Claude (Senior C# & GraphQL Mentor)

---

**Remember:** These aren't just rules—they're guardrails that help us write better code together. When in doubt, ask questions!
