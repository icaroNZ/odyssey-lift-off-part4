# Catstronauts C# Migration Roadmap

## 🗺️ Overview

This roadmap breaks down the migration into **8 distinct stages**, each with **small, incremental tasks** that build upon each other. Each stage ends with all tests passing and a working application.

**Approach:** Test-Driven Development (TDD) + Incremental Delivery

---

## Stage 1: Foundation & Project Setup

**Goal:** Create the C# project structure with all necessary dependencies

**Duration:** 1-2 hours

**Learning Focus:** .NET project structure, NuGet packages, Hot Chocolate setup

### Tasks:

#### 1.1 - Create Solution and Projects
- [ ] Create new solution: `Catstronauts.sln`
- [ ] Create web API project: `Catstronauts.GraphQL`
- [ ] Create test project: `Catstronauts.Tests`
- [ ] Verify projects build successfully

**Commands:**
```bash
dotnet new sln -n Catstronauts
dotnet new web -n Catstronauts.GraphQL
dotnet new xunit -n Catstronauts.Tests
dotnet sln add Catstronauts.GraphQL/Catstronauts.GraphQL.csproj
dotnet sln add Catstronauts.Tests/Catstronauts.Tests.csproj
```

**Success Criteria:**
- ✅ Solution builds without errors
- ✅ Both projects compile successfully

---

#### 1.2 - Install NuGet Packages (Web Project)
- [ ] Install Hot Chocolate core: `HotChocolate.AspNetCore` (v13+)
- [ ] Install HTTP abstractions: `HotChocolate.AspNetCore.Authorization` (optional)
- [ ] Install JSON serialization: Built-in `System.Text.Json`
- [ ] Verify all packages restore correctly

**Commands:**
```bash
cd Catstronauts.GraphQL
dotnet add package HotChocolate.AspNetCore --version 13.*
dotnet restore
```

**Success Criteria:**
- ✅ No package conflicts
- ✅ Project builds with new packages

---

#### 1.3 - Install NuGet Packages (Test Project)
- [ ] Install xUnit: (already included in template)
- [ ] Install NSubstitute: `NSubstitute` (mocking)
- [ ] Install FluentAssertions: `FluentAssertions` (assertions)
- [ ] Install test helpers: `Microsoft.AspNetCore.Mvc.Testing`
- [ ] Add project reference to main project

**Commands:**
```bash
cd ../Catstronauts.Tests
dotnet add package NSubstitute
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add reference ../Catstronauts.GraphQL/Catstronauts.GraphQL.csproj
```

**Success Criteria:**
- ✅ Test project builds
- ✅ Can reference main project classes

---

#### 1.4 - Configure Project Settings
- [ ] Enable nullable reference types in both projects
- [ ] Set C# language version to latest (11 or 12)
- [ ] Configure launch settings for development
- [ ] Set up folder structure (Models, Services, GraphQL)

**Edit `Catstronauts.GraphQL.csproj`:**
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

**Create folder structure:**
```
Catstronauts.GraphQL/
├── Models/
├── Services/
├── GraphQL/
│   ├── Queries/
│   ├── Mutations/
│   ├── Types/
│   └── DataLoaders/
└── Configuration/
```

**Success Criteria:**
- ✅ Folders created
- ✅ Nullable warnings enabled
- ✅ Project compiles with new settings

---

#### 1.5 - Basic "Hello World" GraphQL Endpoint
- [ ] Create minimal `Program.cs` with Hot Chocolate
- [ ] Add simple query: `hello` returning "Hello, GraphQL!"
- [ ] Run application and test with GraphQL IDE (Banana Cake Pop)
- [ ] Verify endpoint works at `http://localhost:5000/graphql`

**Code:**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();

// Simple query class
public class Query
{
    public string GetHello() => "Hello, GraphQL!";
}
```

**Success Criteria:**
- ✅ App starts without errors
- ✅ Can execute query `{ hello }` in browser
- ✅ Returns expected response

---

#### 1.6 - First Test (Sanity Check)
- [ ] Create `BasicTests.cs` in test project
- [ ] Write simple test that verifies 1+1=2
- [ ] Run tests and verify they pass
- [ ] Set up test output configuration

**Code:**
```csharp
public class BasicTests
{
    [Fact]
    public void SanityCheck_OnePlusOne_EqualsTwo()
    {
        // Arrange
        var a = 1;
        var b = 1;

        // Act
        var result = a + b;

        // Assert
        result.Should().Be(2);
    }
}
```

**Commands:**
```bash
dotnet test
```

**Success Criteria:**
- ✅ Test runs and passes
- ✅ Test output is readable

---

**Stage 1 Completion Checklist:**
- [ ] Solution structure created
- [ ] All packages installed
- [ ] Basic GraphQL endpoint working
- [ ] Test project configured
- [ ] At least one test passing
- [ ] No compiler errors or warnings

---

## Stage 2: Data Models & DTOs

**Goal:** Create C# model classes that mirror the GraphQL schema types

**Duration:** 1-2 hours

**Learning Focus:** C# classes, properties, nullable types, POCOs

### Tasks:

#### 2.1 - Create Track Model
- [ ] Create `Models/Track.cs`
- [ ] Add all properties from GraphQL schema
- [ ] Use correct C# types (string, int, etc.)
- [ ] Mark nullable properties appropriately
- [ ] Add XML documentation comments

**Code:**
```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents a learning track (course) with modules and metadata.
/// </summary>
public class Track
{
    /// <summary>
    /// Unique identifier for the track.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The track's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// ID of the track's main author.
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// URL to the track's thumbnail image.
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Approximate length to complete the track, in minutes.
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Number of modules in this track.
    /// </summary>
    public int? ModulesCount { get; set; }

    /// <summary>
    /// Full description of the track (may contain markdown).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of times this track has been viewed.
    /// </summary>
    public int? NumberOfViews { get; set; }
}
```

**Success Criteria:**
- ✅ Class compiles
- ✅ All properties have correct types
- ✅ Nullable properties marked with `?`

---

#### 2.2 - Create Author Model
- [ ] Create `Models/Author.cs`
- [ ] Add properties: Id, Name, Photo
- [ ] Add XML documentation

**Code:**
```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents an author of tracks or modules.
/// </summary>
public class Author
{
    /// <summary>
    /// Unique identifier for the author.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Author's full name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL to the author's profile photo.
    /// </summary>
    public string? Photo { get; set; }
}
```

**Success Criteria:**
- ✅ Class compiles
- ✅ Matches GraphQL schema

---

#### 2.3 - Create Module Model
- [ ] Create `Models/Module.cs`
- [ ] Add properties: Id, Title, Length, Content, VideoUrl
- [ ] Add XML documentation

**Code:**
```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents a single module (lesson) within a track.
/// </summary>
public class Module
{
    /// <summary>
    /// Unique identifier for the module.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The module's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Module duration in minutes.
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Text content or transcript (may contain markdown).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// URL to the module's video (if video-based).
    /// </summary>
    public string? VideoUrl { get; set; }
}
```

**Success Criteria:**
- ✅ Class compiles
- ✅ All properties present

---

#### 2.4 - Create Mutation Response Model
- [ ] Create `Models/IncrementTrackViewsResponse.cs`
- [ ] Add properties: Code, Success, Message, Track
- [ ] This matches the GraphQL mutation response type

**Code:**
```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Response object for the incrementTrackViews mutation.
/// </summary>
public class IncrementTrackViewsResponse
{
    /// <summary>
    /// HTTP-style status code (200 for success, 4xx/5xx for errors).
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Indicates whether the mutation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The updated track, or null if the mutation failed.
    /// </summary>
    public Track? Track { get; set; }
}
```

**Success Criteria:**
- ✅ Class compiles
- ✅ Matches mutation response structure

---

#### 2.5 - Test: Model Instantiation
- [ ] Create `Models/ModelTests.cs`
- [ ] Test creating instances of each model
- [ ] Verify default values
- [ ] Test nullable properties

**Code:**
```csharp
public class ModelTests
{
    [Fact]
    public void Track_CanBeInstantiated_WithDefaultValues()
    {
        // Act
        var track = new Track();

        // Assert
        track.Should().NotBeNull();
        track.Id.Should().BeEmpty();
        track.Title.Should().BeEmpty();
        track.Thumbnail.Should().BeNull();
    }

    [Fact]
    public void Track_CanBeInstantiated_WithValues()
    {
        // Arrange & Act
        var track = new Track
        {
            Id = "t_01",
            Title = "Test Track",
            Length = 120,
            NumberOfViews = 0
        };

        // Assert
        track.Id.Should().Be("t_01");
        track.Title.Should().Be("Test Track");
        track.Length.Should().Be(120);
    }

    // Similar tests for Author, Module, IncrementTrackViewsResponse
}
```

**Success Criteria:**
- ✅ All model tests pass
- ✅ Can create instances successfully

---

**Stage 2 Completion Checklist:**
- [ ] All 4 model classes created
- [ ] All properties match GraphQL schema
- [ ] XML documentation added
- [ ] Unit tests for models pass
- [ ] No compiler warnings

---

## Stage 3: Service Layer (REST API Client)

**Goal:** Create service to communicate with external REST API

**Duration:** 2-3 hours

**Learning Focus:** HttpClient, async/await, dependency injection, error handling

### Tasks:

#### 3.1 - Create Service Interface
- [ ] Create `Services/ITrackService.cs`
- [ ] Define method signatures for all API operations
- [ ] Use async Task<T> return types
- [ ] Add XML documentation

**Code:**
```csharp
namespace Catstronauts.GraphQL.Services;

/// <summary>
/// Service for interacting with the Catstronauts REST API.
/// </summary>
public interface ITrackService
{
    /// <summary>
    /// Retrieves all tracks for the homepage.
    /// </summary>
    Task<List<Track>> GetTracksForHomeAsync();

    /// <summary>
    /// Retrieves a single track by ID.
    /// </summary>
    Task<Track?> GetTrackAsync(string id);

    /// <summary>
    /// Retrieves an author by ID.
    /// </summary>
    Task<Author?> GetAuthorAsync(string id);

    /// <summary>
    /// Retrieves all modules for a specific track.
    /// </summary>
    Task<List<Module>> GetTrackModulesAsync(string trackId);

    /// <summary>
    /// Retrieves a single module by ID.
    /// </summary>
    Task<Module?> GetModuleAsync(string id);

    /// <summary>
    /// Increments the view count for a track.
    /// </summary>
    Task<Track> IncrementTrackViewsAsync(string trackId);
}
```

**Success Criteria:**
- ✅ Interface compiles
- ✅ All methods return Task<T>
- ✅ Nullable types used appropriately

---

#### 3.2 - Implement TrackService (Basic Structure)
- [ ] Create `Services/TrackService.cs`
- [ ] Implement ITrackService interface
- [ ] Add HttpClient dependency via constructor
- [ ] Set base URL for API
- [ ] Stub out all methods (throw NotImplementedException)

**Code:**
```csharp
namespace Catstronauts.GraphQL.Services;

public class TrackService : ITrackService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://odyssey-lift-off-rest-api.herokuapp.com/";

    public TrackService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public Task<List<Track>> GetTracksForHomeAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Track?> GetTrackAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<Author?> GetAuthorAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<List<Module>> GetTrackModulesAsync(string trackId)
    {
        throw new NotImplementedException();
    }

    public Task<Module?> GetModuleAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<Track> IncrementTrackViewsAsync(string trackId)
    {
        throw new NotImplementedException();
    }
}
```

**Success Criteria:**
- ✅ Class compiles
- ✅ Implements interface
- ✅ HttpClient injected

---

#### 3.3 - Register Service in DI Container
- [ ] Update `Program.cs`
- [ ] Register ITrackService with HttpClientFactory
- [ ] Configure service lifetime (Scoped)

**Code:**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register HttpClient with TrackService
builder.Services.AddHttpClient<ITrackService, TrackService>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**Success Criteria:**
- ✅ Service registered
- ✅ App still builds and runs

---

#### 3.4 - Implement GetTracksForHomeAsync
- [ ] Implement method to GET from `/tracks` endpoint
- [ ] Use `GetFromJsonAsync<T>` for deserialization
- [ ] Add error handling
- [ ] Test with actual API call

**Code:**
```csharp
public async Task<List<Track>> GetTracksForHomeAsync()
{
    try
    {
        var tracks = await _httpClient.GetFromJsonAsync<List<Track>>("tracks");
        return tracks ?? new List<Track>();
    }
    catch (HttpRequestException ex)
    {
        // Log error (we'll add logging later)
        throw new InvalidOperationException("Failed to retrieve tracks", ex);
    }
}
```

**Success Criteria:**
- ✅ Method makes real API call
- ✅ Returns list of tracks
- ✅ Error handling works

---

#### 3.5 - Implement Remaining GET Methods
- [ ] Implement `GetTrackAsync(string id)`
- [ ] Implement `GetAuthorAsync(string id)`
- [ ] Implement `GetTrackModulesAsync(string trackId)`
- [ ] Implement `GetModuleAsync(string id)`
- [ ] Handle 404 responses (return null)

**Code:**
```csharp
public async Task<Track?> GetTrackAsync(string id)
{
    if (string.IsNullOrWhiteSpace(id))
        throw new ArgumentException("Track ID cannot be null or empty", nameof(id));

    try
    {
        return await _httpClient.GetFromJsonAsync<Track>($"track/{id}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return null;  // Track not found
    }
}

public async Task<Author?> GetAuthorAsync(string id)
{
    if (string.IsNullOrWhiteSpace(id))
        throw new ArgumentException("Author ID cannot be null or empty", nameof(id));

    try
    {
        return await _httpClient.GetFromJsonAsync<Author>($"author/{id}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return null;
    }
}

public async Task<List<Module>> GetTrackModulesAsync(string trackId)
{
    if (string.IsNullOrWhiteSpace(trackId))
        throw new ArgumentException("Track ID cannot be null or empty", nameof(trackId));

    var modules = await _httpClient.GetFromJsonAsync<List<Module>>($"track/{trackId}/modules");
    return modules ?? new List<Module>();
}

public async Task<Module?> GetModuleAsync(string id)
{
    if (string.IsNullOrWhiteSpace(id))
        throw new ArgumentException("Module ID cannot be null or empty", nameof(id));

    try
    {
        return await _httpClient.GetFromJsonAsync<Module>($"module/{id}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return null;
    }
}
```

**Success Criteria:**
- ✅ All GET methods implemented
- ✅ Proper null checking
- ✅ 404 handling returns null

---

#### 3.6 - Implement IncrementTrackViewsAsync (PATCH)
- [ ] Implement PATCH request to `/track/{id}/numberOfViews`
- [ ] Return updated track
- [ ] Handle errors appropriately

**Code:**
```csharp
public async Task<Track> IncrementTrackViewsAsync(string trackId)
{
    if (string.IsNullOrWhiteSpace(trackId))
        throw new ArgumentException("Track ID cannot be null or empty", nameof(trackId));

    var response = await _httpClient.PatchAsync(
        $"track/{trackId}/numberOfViews",
        null);  // No body needed for this endpoint

    response.EnsureSuccessStatusCode();

    var track = await response.Content.ReadFromJsonAsync<Track>();
    return track ?? throw new InvalidOperationException("API returned null track");
}
```

**Success Criteria:**
- ✅ PATCH request works
- ✅ Returns updated track
- ✅ Throws on errors

---

#### 3.7 - Unit Tests for TrackService
- [ ] Create `Services/TrackServiceTests.cs`
- [ ] Mock HttpClient using HttpMessageHandler
- [ ] Test successful requests
- [ ] Test error scenarios (404, network errors)
- [ ] Test null/empty parameter validation

**Code:**
```csharp
public class TrackServiceTests
{
    [Fact]
    public async Task GetTracksForHomeAsync_WhenSuccessful_ReturnsTracks()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(HttpStatusCode.OK, "[{\"id\":\"t_01\",\"title\":\"Test\"}]");

        var httpClient = new HttpClient(mockHandler);
        var service = new TrackService(httpClient);

        // Act
        var tracks = await service.GetTracksForHomeAsync();

        // Assert
        tracks.Should().NotBeEmpty();
        tracks.Should().HaveCount(1);
        tracks[0].Id.Should().Be("t_01");
    }

    [Fact]
    public async Task GetTrackAsync_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new TrackService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.GetTrackAsync(null!));
    }

    // Add more tests for each method...
}
```

**Note:** We'll need a helper class to mock HttpMessageHandler.

**Success Criteria:**
- ✅ At least 10 tests covering happy path and errors
- ✅ All tests pass

---

**Stage 3 Completion Checklist:**
- [ ] ITrackService interface created
- [ ] TrackService implementation complete
- [ ] All methods implemented and working
- [ ] Service registered in DI container
- [ ] Unit tests written and passing
- [ ] No compiler warnings

---

## Stage 4: GraphQL Queries (Basic)

**Goal:** Expose GraphQL queries that return data from the service

**Duration:** 2-3 hours

**Learning Focus:** Hot Chocolate queries, resolvers, field resolvers

### Tasks:

#### 4.1 - Create Query Class
- [ ] Create `GraphQL/Queries/Query.cs`
- [ ] Add `[Query]` attribute or inherit from ObjectType
- [ ] Inject ITrackService
- [ ] Implement `tracksForHome` query

**Code:**
```csharp
namespace Catstronauts.GraphQL.GraphQL.Queries;

public class Query
{
    /// <summary>
    /// Retrieves all tracks for the homepage grid.
    /// </summary>
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)
    {
        return await trackService.GetTracksForHomeAsync();
    }
}
```

**Success Criteria:**
- ✅ Query class compiles
- ✅ Method signature correct

---

#### 4.2 - Register Query in Hot Chocolate
- [ ] Update `Program.cs` to use new Query class
- [ ] Configure naming conventions (camelCase)
- [ ] Test query in GraphQL IDE

**Code:**
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .ModifyOptions(options =>
    {
        options.DefaultResolverStrategy = ExecutionStrategy.Serial;
    });
```

**Success Criteria:**
- ✅ Query registered
- ✅ Can execute in browser
- ✅ Returns actual data from API

---

#### 4.3 - Add Track Query
- [ ] Add `track(id: ID!)` query to Query class
- [ ] Implement resolver using trackService
- [ ] Handle null case (track not found)
- [ ] Test with valid and invalid IDs

**Code:**
```csharp
/// <summary>
/// Retrieves a specific track by ID.
/// </summary>
public async Task<Track?> GetTrack(
    string id,
    [Service] ITrackService trackService)
{
    return await trackService.GetTrackAsync(id);
}
```

**GraphQL Test:**
```graphql
query {
  track(id: "c_0") {
    id
    title
    description
  }
}
```

**Success Criteria:**
- ✅ Query returns track
- ✅ Returns null for invalid ID
- ✅ No errors

---

#### 4.4 - Add Module Query
- [ ] Add `module(id: ID!)` query
- [ ] Implement resolver
- [ ] Test with valid module ID

**Code:**
```csharp
/// <summary>
/// Retrieves a specific module by ID.
/// </summary>
public async Task<Module?> GetModule(
    string id,
    [Service] ITrackService trackService)
{
    return await trackService.GetModuleAsync(id);
}
```

**Success Criteria:**
- ✅ Module query works
- ✅ Returns correct data

---

#### 4.5 - Create TrackType with Field Resolvers
- [ ] Create `GraphQL/Types/TrackType.cs`
- [ ] Add resolver for `author` field (nested query)
- [ ] Add resolver for `modules` field (nested query)
- [ ] Register type in Hot Chocolate

**Code:**
```csharp
namespace Catstronauts.GraphQL.GraphQL.Types;

[ObjectType("Track")]
public class TrackType
{
    /// <summary>
    /// Resolves the author for a track.
    /// </summary>
    [GraphQLName("author")]
    public async Task<Author?> GetAuthor(
        [Parent] Track track,
        [Service] ITrackService trackService)
    {
        return await trackService.GetAuthorAsync(track.AuthorId);
    }

    /// <summary>
    /// Resolves the modules for a track.
    /// </summary>
    [GraphQLName("modules")]
    public async Task<List<Module>> GetModules(
        [Parent] Track track,
        [Service] ITrackService trackService)
    {
        return await trackService.GetTrackModulesAsync(track.Id);
    }
}
```

**Register in Program.cs:**
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<TrackType>();
```

**Success Criteria:**
- ✅ Can query nested author
- ✅ Can query nested modules
- ✅ Data loads correctly

---

#### 4.6 - Test Complex Nested Query
- [ ] Test query that fetches tracks with authors and modules
- [ ] Verify all data returns correctly
- [ ] Check for N+1 query issues (we'll optimize later)

**GraphQL Test:**
```graphql
query {
  tracksForHome {
    id
    title
    author {
      name
      photo
    }
    modules {
      id
      title
      length
    }
  }
}
```

**Success Criteria:**
- ✅ Query executes successfully
- ✅ All nested data present
- ✅ No errors

---

#### 4.7 - Integration Tests for Queries
- [ ] Create `GraphQL/QueryIntegrationTests.cs`
- [ ] Test each query with real GraphQL execution
- [ ] Verify response structure
- [ ] Test error cases

**Code:**
```csharp
public class QueryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public QueryIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TracksForHome_ReturnsValidData()
    {
        // Arrange
        var client = _factory.CreateClient();
        var query = @"
            query {
                tracksForHome {
                    id
                    title
                }
            }
        ";

        // Act
        var response = await client.PostGraphQLQueryAsync(query);

        // Assert
        response.Should().NotBeNull();
        response.Errors.Should().BeNullOrEmpty();
        response.Data["tracksForHome"].Should().NotBeNull();
    }
}
```

**Success Criteria:**
- ✅ Integration tests pass
- ✅ Queries return expected data

---

**Stage 4 Completion Checklist:**
- [ ] All 3 queries implemented (tracksForHome, track, module)
- [ ] Field resolvers for author and modules working
- [ ] Integration tests passing
- [ ] Can query from GraphQL IDE
- [ ] No N+1 warnings (we'll optimize next stage)

---

## Stage 5: GraphQL Mutations

**Goal:** Implement the incrementTrackViews mutation

**Duration:** 2-3 hours

**Learning Focus:** Mutations, error handling, response types

### Tasks:

#### 5.1 - Create Mutation Class
- [ ] Create `GraphQL/Mutations/Mutation.cs`
- [ ] Add `incrementTrackViews` mutation
- [ ] Return IncrementTrackViewsResponse
- [ ] Handle success and error cases

**Code:**
```csharp
namespace Catstronauts.GraphQL.GraphQL.Mutations;

public class Mutation
{
    /// <summary>
    /// Increments the view count for a track.
    /// </summary>
    public async Task<IncrementTrackViewsResponse> IncrementTrackViews(
        string id,
        [Service] ITrackService trackService)
    {
        try
        {
            var track = await trackService.IncrementTrackViewsAsync(id);

            return new IncrementTrackViewsResponse
            {
                Code = 200,
                Success = true,
                Message = $"Successfully incremented number of views for track {id}",
                Track = track
            };
        }
        catch (HttpRequestException ex)
        {
            return new IncrementTrackViewsResponse
            {
                Code = (int)(ex.StatusCode ?? HttpStatusCode.InternalServerError),
                Success = false,
                Message = ex.Message,
                Track = null
            };
        }
        catch (Exception ex)
        {
            return new IncrementTrackViewsResponse
            {
                Code = 500,
                Success = false,
                Message = $"An error occurred: {ex.Message}",
                Track = null
            };
        }
    }
}
```

**Success Criteria:**
- ✅ Mutation compiles
- ✅ Error handling in place

---

#### 5.2 - Register Mutation in Hot Chocolate
- [ ] Update `Program.cs`
- [ ] Add mutation type to GraphQL server
- [ ] Verify mutation appears in schema

**Code:**
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<TrackType>();
```

**Success Criteria:**
- ✅ Mutation registered
- ✅ Visible in GraphQL IDE schema explorer

---

#### 5.3 - Test Mutation with Valid ID
- [ ] Execute mutation in GraphQL IDE
- [ ] Verify view count increments
- [ ] Check response structure matches schema

**GraphQL Test:**
```graphql
mutation {
  incrementTrackViews(id: "c_0") {
    code
    success
    message
    track {
      id
      numberOfViews
    }
  }
}
```

**Success Criteria:**
- ✅ Mutation executes successfully
- ✅ View count increases
- ✅ Response structure correct

---

#### 5.4 - Test Mutation Error Handling
- [ ] Test with invalid track ID
- [ ] Verify error response structure
- [ ] Ensure success = false and appropriate message

**GraphQL Test:**
```graphql
mutation {
  incrementTrackViews(id: "invalid_id") {
    code
    success
    message
    track {
      id
    }
  }
}
```

**Success Criteria:**
- ✅ Returns error response (not GraphQL error)
- ✅ success = false
- ✅ track = null

---

#### 5.5 - Unit Tests for Mutation
- [ ] Create `GraphQL/MutationTests.cs`
- [ ] Test successful mutation
- [ ] Test error scenarios
- [ ] Mock ITrackService

**Code:**
```csharp
public class MutationTests
{
    [Fact]
    public async Task IncrementTrackViews_WithValidId_ReturnsSuccessResponse()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync("t_01")
            .Returns(new Track { Id = "t_01", NumberOfViews = 100 });

        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("t_01", mockService);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Code.Should().Be(200);
        response.Track.Should().NotBeNull();
        response.Track!.NumberOfViews.Should().Be(100);
    }

    [Fact]
    public async Task IncrementTrackViews_WhenServiceThrows_ReturnsErrorResponse()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.IncrementTrackViewsAsync(Arg.Any<string>())
            .ThrowsAsync(new HttpRequestException("Not found", null, HttpStatusCode.NotFound));

        var mutation = new Mutation();

        // Act
        var response = await mutation.IncrementTrackViews("invalid", mockService);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Code.Should().Be(404);
        response.Track.Should().BeNull();
    }
}
```

**Success Criteria:**
- ✅ Unit tests pass
- ✅ Both success and error paths tested

---

#### 5.6 - Integration Test for Mutation
- [ ] Add mutation integration test
- [ ] Test with real GraphQL execution
- [ ] Verify full request/response cycle

**Code:**
```csharp
[Fact]
public async Task IncrementTrackViews_Integration_Works()
{
    // Arrange
    var client = _factory.CreateClient();
    var mutation = @"
        mutation {
            incrementTrackViews(id: ""c_0"") {
                code
                success
                message
                track {
                    id
                    numberOfViews
                }
            }
        }
    ";

    // Act
    var response = await client.PostGraphQLQueryAsync(mutation);

    // Assert
    response.Errors.Should().BeNullOrEmpty();
    response.Data["incrementTrackViews"]["success"].Should().Be(true);
}
```

**Success Criteria:**
- ✅ Integration test passes
- ✅ Mutation works end-to-end

---

**Stage 5 Completion Checklist:**
- [ ] Mutation implemented
- [ ] Error handling robust
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Mutation works in GraphQL IDE
- [ ] Response structure matches schema

---

## Stage 6: DataLoaders (N+1 Optimization)

**Goal:** Optimize nested queries to prevent N+1 query problem

**Duration:** 2-3 hours

**Learning Focus:** DataLoaders, batching, caching, performance optimization

### Tasks:

#### 6.1 - Understanding the N+1 Problem
- [ ] Document the problem with current implementation
- [ ] Count API calls for query with 10 tracks
- [ ] Explain why this is inefficient

**Example:**
```graphql
query {
  tracksForHome {  # 1 API call
    author {       # N API calls (one per track)
      name
    }
  }
}
# Total: 1 + N calls instead of 2 calls
```

**Success Criteria:**
- ✅ N+1 problem understood
- ✅ Performance issue documented

---

#### 6.2 - Create AuthorDataLoader
- [ ] Create `GraphQL/DataLoaders/AuthorDataLoader.cs`
- [ ] Extend `BatchDataLoader<string, Author>`
- [ ] Implement LoadBatchAsync method
- [ ] Batch fetch all authors in one request

**Code:**
```csharp
namespace Catstronauts.GraphQL.GraphQL.DataLoaders;

public class AuthorDataLoader : BatchDataLoader<string, Author>
{
    private readonly ITrackService _trackService;

    public AuthorDataLoader(
        ITrackService trackService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _trackService = trackService;
    }

    protected override async Task<IReadOnlyDictionary<string, Author>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        // Fetch all authors in parallel
        var authorTasks = keys.Select(id => _trackService.GetAuthorAsync(id));
        var authors = await Task.WhenAll(authorTasks);

        // Return dictionary mapping ID to Author
        return authors
            .Where(a => a != null)
            .ToDictionary(a => a!.Id, a => a!);
    }
}
```

**Note:** For true batching, we'd need a batch endpoint on the API. Since we don't have one, this parallelizes requests.

**Success Criteria:**
- ✅ DataLoader compiles
- ✅ Implements batching logic

---

#### 6.3 - Update TrackType to Use AuthorDataLoader
- [ ] Modify `GetAuthor` method in TrackType
- [ ] Inject and use AuthorDataLoader
- [ ] Test query performance improvement

**Code:**
```csharp
[GraphQLName("author")]
public async Task<Author?> GetAuthor(
    [Parent] Track track,
    AuthorDataLoader authorLoader)  // Changed from ITrackService
{
    return await authorLoader.LoadAsync(track.AuthorId);
}
```

**Success Criteria:**
- ✅ Author loading uses DataLoader
- ✅ Performance improved (fewer concurrent calls)

---

#### 6.4 - Create ModuleDataLoader
- [ ] Create `GraphQL/DataLoaders/ModuleDataLoader.cs`
- [ ] Batch load modules for multiple tracks
- [ ] Handle grouping by track ID

**Code:**
```csharp
public class ModuleDataLoader : GroupedDataLoader<string, Module>
{
    private readonly ITrackService _trackService;

    public ModuleDataLoader(
        ITrackService trackService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _trackService = trackService;
    }

    protected override async Task<ILookup<string, Module>> LoadGroupedBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        // Fetch modules for all track IDs in parallel
        var moduleTasks = keys.Select(async trackId =>
        {
            var modules = await _trackService.GetTrackModulesAsync(trackId);
            return (trackId, modules);
        });

        var results = await Task.WhenAll(moduleTasks);

        // Group modules by track ID
        return results
            .SelectMany(r => r.modules.Select(m => (r.trackId, module: m)))
            .ToLookup(x => x.trackId, x => x.module);
    }
}
```

**Success Criteria:**
- ✅ Module DataLoader compiles
- ✅ Groups modules by track

---

#### 6.5 - Update TrackType to Use ModuleDataLoader
- [ ] Modify `GetModules` method
- [ ] Use ModuleDataLoader
- [ ] Test nested modules query

**Code:**
```csharp
[GraphQLName("modules")]
public async Task<IEnumerable<Module>> GetModules(
    [Parent] Track track,
    ModuleDataLoader moduleLoader)
{
    return await moduleLoader.LoadAsync(track.Id);
}
```

**Success Criteria:**
- ✅ Modules load via DataLoader
- ✅ Query still works correctly

---

#### 6.6 - Register DataLoaders in Hot Chocolate
- [ ] Update `Program.cs`
- [ ] Add DataLoader registration
- [ ] Configure pooling if needed

**Code:**
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<TrackType>()
    .AddDataLoader<AuthorDataLoader>()
    .AddDataLoader<ModuleDataLoader>();
```

**Success Criteria:**
- ✅ DataLoaders registered
- ✅ Application builds and runs

---

#### 6.7 - Performance Testing
- [ ] Create benchmark query with 10 tracks + authors + modules
- [ ] Measure performance before/after DataLoaders
- [ ] Document improvement
- [ ] Ensure correctness maintained

**GraphQL Test:**
```graphql
query {
  tracksForHome {
    id
    title
    author {
      id
      name
      photo
    }
    modules {
      id
      title
      length
    }
  }
}
```

**Success Criteria:**
- ✅ Query returns same data
- ✅ Fewer API calls made
- ✅ Performance improved

---

**Stage 6 Completion Checklist:**
- [ ] N+1 problem understood and documented
- [ ] AuthorDataLoader implemented
- [ ] ModuleDataLoader implemented
- [ ] DataLoaders registered and working
- [ ] Performance improvement measured
- [ ] All tests still passing

---

## Stage 7: Comprehensive Testing

**Goal:** Achieve 80%+ code coverage with robust tests

**Duration:** 3-4 hours

**Learning Focus:** Test strategies, mocking, coverage analysis

### Tasks:

#### 7.1 - Install Code Coverage Tools
- [ ] Install coverlet.collector
- [ ] Install ReportGenerator (optional, for HTML reports)
- [ ] Configure test project for coverage

**Commands:**
```bash
dotnet add package coverlet.collector
dotnet tool install -g dotnet-reportgenerator-globaltool
```

**Success Criteria:**
- ✅ Coverage tools installed
- ✅ Can generate coverage report

---

#### 7.2 - Expand Service Tests
- [ ] Add tests for all TrackService methods
- [ ] Test edge cases: empty strings, nulls, invalid IDs
- [ ] Test network errors, timeouts
- [ ] Test successful responses

**Target:** 90%+ coverage of TrackService

**Success Criteria:**
- ✅ Comprehensive service tests
- ✅ All edge cases covered

---

#### 7.3 - Expand Query Tests
- [ ] Test all query resolvers
- [ ] Test with mocked services
- [ ] Test null handling
- [ ] Test exception propagation

**Success Criteria:**
- ✅ Query class fully tested
- ✅ All paths covered

---

#### 7.4 - Expand Mutation Tests
- [ ] Test all success scenarios
- [ ] Test all error scenarios
- [ ] Test validation
- [ ] Test response structure

**Success Criteria:**
- ✅ Mutation fully tested
- ✅ Error handling validated

---

#### 7.5 - DataLoader Tests
- [ ] Test AuthorDataLoader batching
- [ ] Test ModuleDataLoader grouping
- [ ] Test empty results
- [ ] Test cancellation

**Code:**
```csharp
public class AuthorDataLoaderTests
{
    [Fact]
    public async Task LoadBatchAsync_WithMultipleIds_ReturnsAuthors()
    {
        // Arrange
        var mockService = Substitute.For<ITrackService>();
        mockService.GetAuthorAsync("a_01").Returns(new Author { Id = "a_01" });
        mockService.GetAuthorAsync("a_02").Returns(new Author { Id = "a_02" });

        var scheduler = new BatchScheduler();
        var loader = new AuthorDataLoader(mockService, scheduler);

        // Act
        var result = await loader.LoadAsync(new[] { "a_01", "a_02" });

        // Assert
        result.Should().HaveCount(2);
    }
}
```

**Success Criteria:**
- ✅ DataLoader tests pass
- ✅ Batching behavior verified

---

#### 7.6 - Integration Tests for All Endpoints
- [ ] Test tracksForHome query
- [ ] Test track query with nested data
- [ ] Test module query
- [ ] Test incrementTrackViews mutation
- [ ] Test error scenarios

**Success Criteria:**
- ✅ Full integration test suite
- ✅ All endpoints tested

---

#### 7.7 - Run Coverage Report
- [ ] Generate coverage report
- [ ] Analyze coverage percentages
- [ ] Identify untested code
- [ ] Add tests to reach 80%+

**Commands:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report
```

**Success Criteria:**
- ✅ Coverage report generated
- ✅ 80%+ coverage achieved
- ✅ All critical paths tested

---

#### 7.8 - Test Documentation
- [ ] Document test strategy in README
- [ ] Add comments to complex tests
- [ ] Create test data fixtures if needed

**Success Criteria:**
- ✅ Tests are documented
- ✅ Easy for others to understand

---

**Stage 7 Completion Checklist:**
- [ ] 80%+ code coverage achieved
- [ ] All critical functionality tested
- [ ] Integration tests comprehensive
- [ ] Edge cases covered
- [ ] All tests passing
- [ ] Coverage report generated

---

## Stage 8: Refinement & Documentation

**Goal:** Polish the application, add logging, documentation, and prepare for deployment

**Duration:** 1-2 hours

**Learning Focus:** Logging, configuration, documentation, deployment prep

### Tasks:

#### 8.1 - Add Logging
- [ ] Configure Serilog or built-in logging
- [ ] Add logging to TrackService
- [ ] Add logging to mutations
- [ ] Log errors appropriately

**Code:**
```csharp
public class TrackService : ITrackService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrackService> _logger;

    public TrackService(HttpClient httpClient, ILogger<TrackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<Track>> GetTracksForHomeAsync()
    {
        _logger.LogInformation("Fetching tracks for home page");

        try
        {
            var tracks = await _httpClient.GetFromJsonAsync<List<Track>>("tracks");
            _logger.LogInformation("Successfully fetched {Count} tracks", tracks?.Count ?? 0);
            return tracks ?? new List<Track>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tracks");
            throw;
        }
    }
}
```

**Success Criteria:**
- ✅ Logging configured
- ✅ Important events logged
- ✅ Errors logged with context

---

#### 8.2 - Configuration Management
- [ ] Create appsettings.json
- [ ] Externalize API base URL
- [ ] Add configuration for timeouts, retries
- [ ] Support environment-specific settings

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "TrackApi": {
    "BaseUrl": "https://odyssey-lift-off-rest-api.herokuapp.com/",
    "TimeoutSeconds": 30
  }
}
```

**Success Criteria:**
- ✅ Configuration externalized
- ✅ Can override per environment

---

#### 8.3 - Error Handling Improvements
- [ ] Add global error filter for GraphQL
- [ ] Improve error messages
- [ ] Ensure no sensitive data in errors

**Code:**
```csharp
public class ErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        // Log error details
        // Return sanitized error to client
        return error.WithMessage("An error occurred processing your request");
    }
}
```

**Success Criteria:**
- ✅ Error handling centralized
- ✅ User-friendly error messages

---

#### 8.4 - Create README.md
- [ ] Document what the application does
- [ ] Add setup instructions
- [ ] Add run instructions
- [ ] Document API endpoints

**README.md Structure:**
```markdown
# Catstronauts GraphQL Server (C#)

## Overview
GraphQL server for Catstronauts learning platform, built with .NET 8 and Hot Chocolate.

## Prerequisites
- .NET 8 SDK
- Visual Studio Code or Visual Studio 2022

## Setup
1. Clone repository
2. Run `dotnet restore`
3. Run `dotnet run --project Catstronauts.GraphQL`

## Testing
`dotnet test`

## GraphQL Endpoint
http://localhost:5000/graphql

## Architecture
[Diagram and explanation]
```

**Success Criteria:**
- ✅ README complete
- ✅ Clear setup instructions

---

#### 8.5 - Add XML Documentation
- [ ] Enable XML doc generation
- [ ] Ensure all public APIs documented
- [ ] Generate documentation comments

**In .csproj:**
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

**Success Criteria:**
- ✅ XML docs generated
- ✅ No missing documentation warnings

---

#### 8.6 - Code Cleanup
- [ ] Remove unused usings
- [ ] Format code consistently
- [ ] Fix any compiler warnings
- [ ] Run code analyzer

**Commands:**
```bash
dotnet format
dotnet build -warnaserror
```

**Success Criteria:**
- ✅ No warnings
- ✅ Code formatted
- ✅ Clean build

---

#### 8.7 - Performance Baseline
- [ ] Document typical query response times
- [ ] Document API call counts with DataLoaders
- [ ] Create performance benchmarks (optional)

**Success Criteria:**
- ✅ Performance documented
- ✅ Baseline established

---

#### 8.8 - Final Integration Test
- [ ] Run full test suite
- [ ] Test against real API
- [ ] Verify frontend compatibility (if possible)
- [ ] Smoke test all endpoints

**Success Criteria:**
- ✅ All tests pass
- ✅ Application works end-to-end
- ✅ Ready for frontend integration

---

**Stage 8 Completion Checklist:**
- [ ] Logging implemented
- [ ] Configuration externalized
- [ ] Error handling robust
- [ ] README complete
- [ ] Code clean and formatted
- [ ] Documentation complete
- [ ] All tests passing
- [ ] Application production-ready

---

## 🎓 Learning Summary

After completing all stages, you will have learned:

### C# Skills
- ✅ Project structure and organization
- ✅ Dependency injection
- ✅ Async/await patterns
- ✅ Interface-based design
- ✅ Nullable reference types
- ✅ Modern C# features

### GraphQL Skills
- ✅ Schema design
- ✅ Queries and mutations
- ✅ Resolvers and field resolvers
- ✅ DataLoaders for optimization
- ✅ Error handling strategies

### Hot Chocolate Specifics
- ✅ Code-first schema definition
- ✅ Attribute-based configuration
- ✅ ASP.NET Core integration
- ✅ Performance optimization

### Software Engineering
- ✅ SOLID principles application
- ✅ Test-driven development
- ✅ Clean code practices
- ✅ Documentation standards

### Testing
- ✅ Unit testing with xUnit
- ✅ Integration testing
- ✅ Mocking with NSubstitute
- ✅ Code coverage analysis

---

## 📊 Progress Tracking

| Stage | Status | Tests Passing | Coverage |
|-------|--------|---------------|----------|
| 1. Foundation | ⬜ Not Started | - | - |
| 2. Models | ⬜ Not Started | - | - |
| 3. Service Layer | ⬜ Not Started | - | - |
| 4. Queries | ⬜ Not Started | - | - |
| 5. Mutations | ⬜ Not Started | - | - |
| 6. DataLoaders | ⬜ Not Started | - | - |
| 7. Testing | ⬜ Not Started | - | - |
| 8. Refinement | ⬜ Not Started | - | - |

**Legend:**
- ⬜ Not Started
- 🟡 In Progress
- ✅ Completed

---

## 🚀 Next Steps

1. Review this roadmap
2. Ask any questions about the approach
3. Start with Stage 1: Foundation & Project Setup
4. Work through each stage incrementally
5. Ensure all tests pass before moving to next stage

---

**Remember:**
- 🎯 Small, incremental steps
- ✅ Tests must pass at each stage
- 💬 Ask questions before proceeding
- 📚 Learn and understand, don't just copy

Let's build something great together! 🚀

---

**Document Version:** 1.0
**Last Updated:** 2025-11-19
**Author:** Claude (Senior C# & GraphQL Mentor)
