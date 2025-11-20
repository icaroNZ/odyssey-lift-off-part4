# Stage 4: GraphQL Queries

## Overview

In Stage 4, we implemented **GraphQL queries** - the heart of our GraphQL API. We created query resolvers that allow clients to fetch tracks, authors, and modules using GraphQL's flexible query language.

**What we built:**
- Updated `Query` class with 4 resolver methods
- `tracksForHome` - Fetch all tracks
- `track(id)` - Fetch a single track
- `author` field resolver - Fetch track's author (nested)
- `modules` field resolver - Fetch track's modules (nested)

**Why this matters:**
This is where GraphQL's power becomes evident. Clients can request exactly the data they need in a single query, and field resolvers enable lazy loading and nested data fetching.

---

## Learning Objectives

By the end of this stage, you will understand:

1. ✅ What GraphQL queries are and how they differ from mutations
2. ✅ How Hot Chocolate maps C# methods to GraphQL fields
3. ✅ The `[Service]` attribute for dependency injection in resolvers
4. ✅ Field resolvers and the `[Parent]` attribute
5. ✅ The `[GraphQLName]` attribute for field naming
6. ✅ Lazy loading in GraphQL
7. ✅ Why we use `AuthorId` instead of `Author` object
8. ✅ The N+1 problem (and its solution in Stage 6)
9. ✅ Testing GraphQL resolvers with mocks

---

## Key Concepts

### Concept 1: GraphQL Queries vs Mutations

**GraphQL has three operation types:**

| Operation | Purpose | Example | Side Effects |
|-----------|---------|---------|--------------|
| **Query** | Read data | `query { tracks { id } }` | None (idempotent) |
| **Mutation** | Modify data | `mutation { incrementViews(id: "c_0") }` | Changes server state |
| **Subscription** | Real-time updates | `subscription { trackUpdated }` | Stream of events |

**Queries:**
- Read-only operations
- Can run in parallel (order doesn't matter)
- Should be idempotent (calling multiple times has same result)
- Examples: Get user profile, list products, search

**Mutations:**
- Write operations
- Run sequentially (order matters!)
- Not idempotent (calling twice might have different effect)
- Examples: Create user, update profile, delete item

**Interview Answer:**
> "GraphQL queries are read-only operations that fetch data without side effects. Mutations modify data and have side effects. GraphQL enforces this distinction - queries can run in parallel for performance, but mutations run sequentially to ensure correct ordering. In our API, `tracksForHome` is a query (read tracks), while `incrementTrackViews` is a mutation (modifies view count)."

---

### Concept 2: Hot Chocolate Method Naming Conventions

**Automatic Field Name Generation:**

Hot Chocolate automatically maps C# methods to GraphQL fields:

```csharp
// C# method
public async Task<List<Track>> GetTracksForHome()

// Becomes GraphQL field:
// tracksForHome: [Track!]!
//  - Removes "Get" prefix
//  - Converts to camelCase
```

**Rules:**
1. Remove "Get" prefix (if present)
2. Convert to camelCase (first letter lowercase)
3. Method parameters become field arguments

**Examples:**

| C# Method | GraphQL Field |
|-----------|---------------|
| `GetTracksForHome()` | `tracksForHome` |
| `GetTrack(string id)` | `track(id: String!)` |
| `GetUserProfile(int userId)` | `userProfile(userId: Int!)` |
| `SearchProducts(string query)` | `searchProducts(query: String!)` |

**You can override with [GraphQLName]:**

```csharp
[GraphQLName("allTracks")]
public async Task<List<Track>> GetTracksForHome()
// GraphQL field: allTracks (not tracksForHome)
```

**Interview Answer:**
> "Hot Chocolate uses convention over configuration. Methods starting with 'Get' have that prefix removed and are converted to camelCase for the GraphQL field name. This keeps C# code following C# conventions (PascalCase) while GraphQL follows GraphQL conventions (camelCase). You can override with [GraphQLName] attribute if needed."

---

### Concept 3: The [Service] Attribute - Dependency Injection in Resolvers

**What is [Service]?**

The `[Service]` attribute tells Hot Chocolate to inject a dependency from the DI container into a resolver method.

**Without [Service] (Won't work):**
```csharp
public async Task<List<Track>> GetTracksForHome(
    ITrackService trackService)  // ❌ Hot Chocolate doesn't know what this is
{
    return await trackService.GetTracksForHomeAsync();
}
```

**With [Service] (Works!):**
```csharp
public async Task<List<Track>> GetTracksForHome(
    [Service] ITrackService trackService)  // ✅ Hot Chocolate injects from DI
{
    return await trackService.GetTracksForHomeAsync();
}
```

**How it works:**

```
1. GraphQL query arrives: { tracksForHome { id } }
2. Hot Chocolate calls GetTracksForHome()
3. Sees parameter with [Service] attribute
4. Asks DI container: "Give me ITrackService"
5. Container provides TrackService instance
6. Hot Chocolate calls method with injected service
7. Method executes, returns tracks
```

**Other resolver attributes:**
- `[Service]` - Inject from DI container
- `[Parent]` - Inject parent object (for field resolvers)
- `[GlobalState]` - Inject global state (request-scoped data)
- `[ScopedState]` - Inject scoped state
- `[LocalState]` - Inject local state

**Interview Answer:**
> "The [Service] attribute enables dependency injection in GraphQL resolvers. Hot Chocolate sees the attribute and resolves the parameter from the DI container. This lets us inject services, DataLoaders, or any other registered dependencies directly into resolver methods, keeping resolvers decoupled and testable."

---

### Concept 4: Field Resolvers and the [Parent] Attribute

**What is a Field Resolver?**

A field resolver is a method that resolves a specific field on a GraphQL type. It's called automatically when a client requests that field.

**The Problem We're Solving:**

Our `Track` model has `AuthorId` (string), but clients want the full `Author` object:

```graphql
query {
  track(id: "c_0") {
    title
    author {        # ← Want Author object, not just ID!
      name
      photo
    }
  }
}
```

**Solution: Field Resolver**

```csharp
// Track model (Stage 2)
public class Track
{
    public string AuthorId { get; set; }  // Just the ID
    // No Author property!
}

// Query class (Stage 4)
[GraphQLName("author")]  // Maps to "author" field
public async Task<Author?> GetAuthor(
    [Parent] Track track,  // ← The parent Track object
    [Service] ITrackService trackService)
{
    // Use track.AuthorId to fetch the Author
    return await trackService.GetAuthorAsync(track.AuthorId);
}
```

**Execution Flow:**

```
Client Query:
query {
  track(id: "c_0") {
    title           # ← From Track object
    author {        # ← Triggers field resolver
      name          # ← From Author object
    }
  }
}

Hot Chocolate Execution:
1. Client requests track
2. Calls GetTrack("c_0") → Returns Track object
3. Client requested "author" field
4. Looks for field resolver with [GraphQLName("author")]
5. Calls GetAuthor(track, trackService)
   - track = the Track from step 2 ([Parent])
   - trackService = injected from DI ([Service])
6. GetAuthor uses track.AuthorId to fetch Author
7. Returns Author object
8. GraphQL extracts "name" field from Author
9. Sends response to client
```

**Why This Pattern?**

**Benefits:**
- ✅ **Lazy loading**: Only fetch author if client requests it
- ✅ **Flexibility**: Can fetch from different source than Track
- ✅ **DataLoader support**: Easy to optimize with batching (Stage 6)
- ✅ **Separation**: Track data and Author data fetched independently

**Comparison:**

**Without Field Resolver (Bad):**
```csharp
public class Track
{
    public Author Author { get; set; }  // ❌ Always fetched, even if not needed
}

// Must fetch author with every track, waste of resources!
```

**With Field Resolver (Good):**
```csharp
public class Track
{
    public string AuthorId { get; set; }  // ✅ Just store ID
}

// Fetch author only when client requests it!
[GraphQLName("author")]
public async Task<Author?> GetAuthor([Parent] Track track, ...)
```

**Interview Answer:**
> "Field resolvers let us define how to fetch nested data in GraphQL. Instead of eagerly loading the Author with every Track, we store just the AuthorId. When a client requests the author field, Hot Chocolate calls our field resolver, passing the parent Track object via [Parent] attribute. The resolver uses track.AuthorId to fetch the Author. This enables lazy loading - we only fetch data the client actually requested, improving performance."

---

### Concept 5: The [GraphQLName] Attribute

**What is [GraphQLName]?**

It explicitly sets the GraphQL field name, overriding Hot Chocolate's convention.

**Why We Need It:**

```csharp
// Without [GraphQLName]
public async Task<Author?> GetAuthor([Parent] Track track, ...)

// Hot Chocolate thinks:
// - Method name: GetAuthor
// - Remove "Get": Author
// - camelCase: author
// ✅ Field name: author (correct!)

// But on Track type? Hot Chocolate doesn't know this is a field resolver!
// It would think GetAuthor is a root query, not a field on Track.
```

**Solution:**

```csharp
[GraphQLName("author")]  // ← Explicitly: "This is the 'author' field"
public async Task<Author?> GetAuthor([Parent] Track track, ...)
```

**Effect:**

```graphql
type Track {
  id: String!
  title: String!
  author: Author   # ← This field is resolved by GetAuthor method
}
```

**Other Uses:**

```csharp
// Rename to match GraphQL conventions
[GraphQLName("numberOfViews")]
public int ViewCount { get; set; }  // Property named differently in C#

// Avoid reserved keywords
[GraphQLName("type")]
public string ItemType { get; set; }  // "type" is reserved in C#
```

**Interview Answer:**
> "The [GraphQLName] attribute explicitly sets the GraphQL field name. For field resolvers, it tells Hot Chocolate 'this method resolves the author field on the Track type.' It's also useful for matching GraphQL naming conventions (camelCase) when C# uses different conventions, or avoiding reserved keywords."

---

### Concept 6: Lazy Loading in GraphQL

**What is Lazy Loading?**

Fetching data only when it's actually requested, not upfront.

**Example:**

**Query 1 (Just track info):**
```graphql
query {
  track(id: "c_0") {
    title
    length
  }
}
```

**Execution:**
```
1. GetTrack("c_0") → Fetches track from API
2. Client didn't request author → GetAuthor NOT called
3. Client didn't request modules → GetModules NOT called
Result: 1 API call (track only)
```

**Query 2 (With author):**
```graphql
query {
  track(id: "c_0") {
    title
    author { name }
  }
}
```

**Execution:**
```
1. GetTrack("c_0") → Fetches track
2. Client requested author → GetAuthor called
3. Client didn't request modules → GetModules NOT called
Result: 2 API calls (track + author)
```

**Query 3 (Everything):**
```graphql
query {
  track(id: "c_0") {
    title
    author { name }
    modules { title }
  }
}
```

**Execution:**
```
1. GetTrack("c_0") → Fetches track
2. Client requested author → GetAuthor called
3. Client requested modules → GetModules called
Result: 3 API calls (track + author + modules)
```

**Benefits:**
- ✅ **Performance**: Only fetch what's needed
- ✅ **Bandwidth**: Less data transferred
- ✅ **Flexibility**: Client controls data fetching

**Contrast with REST:**

**REST (Over-fetching):**
```
GET /api/track/c_0

Response (always includes everything):
{
  "id": "c_0",
  "title": "Catstronauts",
  "author": { ... },      // ← Fetched even if not needed
  "modules": [ ... ]      // ← Fetched even if not needed
}
```

**GraphQL (Lazy Loading):**
```graphql
# Client 1: Just basic info
query { track { title } }  # Only fetches track

# Client 2: Needs author
query { track { title author { name } } }  # Fetches track + author

# Client 3: Needs everything
query { track { title author { name } modules { title } } }  # Fetches all
```

**Interview Answer:**
> "Lazy loading in GraphQL means data is only fetched when the client explicitly requests it. Field resolvers enable this - the author field is only resolved if the query includes it. This prevents over-fetching (sending data the client doesn't need) and under-fetching (requiring multiple requests). Clients get exactly what they ask for in a single round trip."

---

### Concept 7: The N+1 Problem (Preview)

**What is the N+1 Problem?**

When fetching nested data results in N additional queries for N parent objects.

**Example:**

**Query:**
```graphql
query {
  tracksForHome {        # 10 tracks
    title
    author { name }      # ← Each track needs its author
  }
}
```

**Without Optimization:**
```
1. GetTracksForHome() → Fetches 10 tracks (1 query)
2. For track 1: GetAuthor(track1.AuthorId) → 1 query
3. For track 2: GetAuthor(track2.AuthorId) → 1 query
4. ...
5. For track 10: GetAuthor(track10.AuthorId) → 1 query

Total: 1 + 10 = 11 queries!
```

**The Problem:**
- If 10 tracks: 11 queries
- If 100 tracks: 101 queries
- If 1000 tracks: 1001 queries!

**Solution: DataLoaders (Stage 6)**

DataLoaders batch and cache requests:
```
1. GetTracksForHome() → Fetches 10 tracks
2. DataLoader collects all author IDs: [cat-1, cat-2, cat-1, cat-3]
3. Deduplicates: [cat-1, cat-2, cat-3]
4. Fetches all 3 authors in parallel (or 1 batch query)
5. Caches results

Total: 1 track query + 1 batch author query = 2 queries!
```

**Note:** In Stage 4, we have the N+1 problem. Stage 6 solves it with DataLoaders.

**Interview Answer:**
> "The N+1 problem occurs when querying nested data results in N separate queries for N parent objects. If you query 100 tracks with authors, you get 1 track query plus 100 author queries. The solution is DataLoaders, which batch requests and cache results. Instead of 101 queries, you get 2 - one for tracks, one batched query for unique authors. We'll implement this in Stage 6."

---

## Step-by-Step Implementation

### Step 1: Update Query Class with Top-Level Queries

**File:** `GraphQL/Queries/Query.cs`

```csharp
using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;

namespace Catstronauts.GraphQL.GraphQL.Queries;

public class Query
{
    /// <summary>
    /// Retrieves all tracks for display on the homepage.
    /// </summary>
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)
    {
        return await trackService.GetTracksForHomeAsync();
    }

    /// <summary>
    /// Retrieves a single track by its unique identifier.
    /// </summary>
    public async Task<Track?> GetTrack(
        string id,
        [Service] ITrackService trackService)
    {
        return await trackService.GetTrackAsync(id);
    }
}
```

**GraphQL Schema Generated:**
```graphql
type Query {
  tracksForHome: [Track!]!
  track(id: String!): Track
}
```

**Example Queries:**

```graphql
# Get all tracks
query GetAllTracks {
  tracksForHome {
    id
    title
    thumbnail
  }
}

# Get one track
query GetOneTrack {
  track(id: "c_0") {
    id
    title
    description
  }
}
```

---

### Step 2: Add Author Field Resolver

**Add to Query class:**

```csharp
/// <summary>
/// Field resolver for the 'author' field on the Track type.
/// </summary>
[GraphQLName("author")]
public async Task<Author?> GetAuthor(
    [Parent] Track track,
    [Service] ITrackService trackService)
{
    return await trackService.GetAuthorAsync(track.AuthorId);
}
```

**GraphQL Schema Generated:**
```graphql
type Track {
  id: String!
  title: String!
  authorId: String!
  author: Author     # ← Added by field resolver!
  # ... other fields
}

type Author {
  id: String!
  name: String!
  photo: String
}
```

**Example Query:**
```graphql
query GetTrackWithAuthor {
  track(id: "c_0") {
    id
    title
    author {         # ← Resolved by GetAuthor method
      id
      name
      photo
    }
  }
}
```

**Execution Flow:**
1. Client requests track with author
2. Hot Chocolate calls `GetTrack("c_0")`
3. Returns Track: `{ id: "c_0", title: "...", authorId: "cat-1" }`
4. Client requested `author` field
5. Hot Chocolate finds field resolver with `[GraphQLName("author")]`
6. Calls `GetAuthor(track, trackService)`
   - `track` = the Track object from step 3
   - `track.AuthorId` = "cat-1"
7. `trackService.GetAuthorAsync("cat-1")` fetches author
8. Returns Author: `{ id: "cat-1", name: "Henri", photo: "..." }`
9. GraphQL response includes both track and author data

---

### Step 3: Add Modules Field Resolver

**Add to Query class:**

```csharp
/// <summary>
/// Field resolver for the 'modules' field on the Track type.
/// </summary>
[GraphQLName("modules")]
public async Task<List<Module>> GetModules(
    [Parent] Track track,
    [Service] ITrackService trackService)
{
    return await trackService.GetTrackModulesAsync(track.Id);
}
```

**GraphQL Schema Generated:**
```graphql
type Track {
  id: String!
  title: String!
  modules: [Module!]!   # ← Added by field resolver!
  # ... other fields
}

type Module {
  id: String!
  title: String!
  length: Int
  content: String
  videoUrl: String
}
```

**Example Query:**
```graphql
query GetTrackWithModules {
  track(id: "c_0") {
    id
    title
    modules {        # ← Resolved by GetModules method
      id
      title
      length
    }
  }
}
```

---

### Step 4: Complete Query Class

**Final `Query.cs`:**

```csharp
using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;

namespace Catstronauts.GraphQL.GraphQL.Queries;

public class Query
{
    // Top-level queries
    public async Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)
    {
        return await trackService.GetTracksForHomeAsync();
    }

    public async Task<Track?> GetTrack(
        string id,
        [Service] ITrackService trackService)
    {
        return await trackService.GetTrackAsync(id);
    }

    // Field resolvers (nested data)
    [GraphQLName("author")]
    public async Task<Author?> GetAuthor(
        [Parent] Track track,
        [Service] ITrackService trackService)
    {
        return await trackService.GetAuthorAsync(track.AuthorId);
    }

    [GraphQLName("modules")]
    public async Task<List<Module>> GetModules(
        [Parent] Track track,
        [Service] ITrackService trackService)
    {
        return await trackService.GetTrackModulesAsync(track.Id);
    }
}
```

**Complete GraphQL Schema:**
```graphql
type Query {
  tracksForHome: [Track!]!
  track(id: String!): Track
}

type Track {
  id: String!
  title: String!
  authorId: String!
  thumbnail: String
  length: Int
  modulesCount: Int
  description: String
  numberOfViews: Int
  author: Author           # Field resolver
  modules: [Module!]!      # Field resolver
}

type Author {
  id: String!
  name: String!
  photo: String
}

type Module {
  id: String!
  title: String!
  length: Int
  content: String
  videoUrl: String
}
```

---

### Step 5: Write Comprehensive Tests

**File:** `Catstronauts.Tests/GraphQL/Queries/QueryTests.cs`

We created **21 tests** covering:

**TracksForHome Tests (3):**
```csharp
[Fact]
public async Task GetTracksForHome_WithAvailableTracks_ReturnsListOfTracks()
{
    // Arrange
    var mockService = Substitute.For<ITrackService>();
    var expectedTracks = new List<Track>
    {
        new Track { Id = "c_0", Title = "Catstronauts" },
        new Track { Id = "c_1", Title = "Advanced GraphQL" }
    };
    mockService.GetTracksForHomeAsync().Returns(expectedTracks);
    var query = new Query();

    // Act
    var result = await query.GetTracksForHome(mockService);

    // Assert
    result.Should().HaveCount(2);
    result.Should().BeEquivalentTo(expectedTracks);
    await mockService.Received(1).GetTracksForHomeAsync();
}
```

**Track Query Tests (4):**
```csharp
[Fact]
public async Task GetTrack_WithValidId_ReturnsTrack()
{
    var mockService = Substitute.For<ITrackService>();
    var expectedTrack = new Track { Id = "c_0", Title = "Catstronauts" };
    mockService.GetTrackAsync("c_0").Returns(expectedTrack);
    var query = new Query();

    var result = await query.GetTrack("c_0", mockService);

    result.Should().BeEquivalentTo(expectedTrack);
}

[Fact]
public async Task GetTrack_WithNonExistentId_ReturnsNull()
{
    var mockService = Substitute.For<ITrackService>();
    mockService.GetTrackAsync("nonexistent").Returns((Track?)null);
    var query = new Query();

    var result = await query.GetTrack("nonexistent", mockService);

    result.Should().BeNull();
}
```

**Author Field Resolver Tests (3):**
```csharp
[Fact]
public async Task GetAuthor_WithValidTrack_ReturnsAuthor()
{
    var mockService = Substitute.For<ITrackService>();
    var track = new Track { Id = "c_0", AuthorId = "cat-1" };
    var expectedAuthor = new Author { Id = "cat-1", Name = "Henri" };
    mockService.GetAuthorAsync("cat-1").Returns(expectedAuthor);
    var query = new Query();

    var result = await query.GetAuthor(track, mockService);

    result.Should().BeEquivalentTo(expectedAuthor);
    await mockService.Received(1).GetAuthorAsync("cat-1");
}
```

**Modules Field Resolver Tests (3):**
```csharp
[Fact]
public async Task GetModules_WithValidTrack_ReturnsModules()
{
    var mockService = Substitute.For<ITrackService>();
    var track = new Track { Id = "c_0" };
    var expectedModules = new List<Module>
    {
        new Module { Id = "l_0", Title = "Introduction" }
    };
    mockService.GetTrackModulesAsync("c_0").Returns(expectedModules);
    var query = new Query();

    var result = await query.GetModules(track, mockService);

    result.Should().BeEquivalentTo(expectedModules);
}
```

**Type Verification Tests (4):**
```csharp
[Fact]
public void GetTracksForHome_ReturnsTaskOfListOfTrack()
{
    var method = typeof(Query).GetMethod(nameof(Query.GetTracksForHome));
    method!.ReturnType.Should().Be(typeof(Task<List<Track>>));
}
```

---

## Design Decisions & Trade-offs

### Decision 1: Field Resolvers vs Embedded Objects

**What we chose:** Field resolvers with `AuthorId`

**Alternative:** Embed full `Author` object in `Track`

**Why field resolvers?**
- ✅ Lazy loading (only fetch when requested)
- ✅ Flexible (can fetch from different sources)
- ✅ DataLoader support (Stage 6)
- ✅ Cleaner data models

**Trade-off:**
- ❌ More complex initially
- ❌ N+1 problem (until Stage 6)

---

### Decision 2: [Service] vs Constructor Injection

**What we chose:** `[Service]` attribute in method parameters

**Alternative:** Constructor injection

```csharp
// Alternative (constructor injection)
public class Query
{
    private readonly ITrackService _trackService;

    public Query(ITrackService trackService)
    {
        _trackService = trackService;
    }

    public async Task<List<Track>> GetTracksForHome()
    {
        return await _trackService.GetTracksForHomeAsync();
    }
}
```

**Why [Service] in parameters?**
- ✅ More flexible (different services per method)
- ✅ Hot Chocolate convention
- ✅ Clearer which service each method needs

**Trade-off:**
- ❌ Slightly more verbose

---

## Interview Preparation

### Q1: Explain how field resolvers work in GraphQL.

**Answer:**
"Field resolvers are methods that fetch data for specific fields on a GraphQL type. They enable lazy loading and nested data fetching.

**Example:**

```graphql
query {
  track(id: 'c_0') {
    title           # From Track object
    author { name } # Resolved by field resolver
  }
}
```

**Execution:**
1. Client requests track with author
2. Hot Chocolate calls `GetTrack('c_0')` → Returns Track object
3. Client requested `author` field
4. Hot Chocolate finds field resolver: `GetAuthor([Parent] Track track)`
5. Calls resolver, passing the Track as `[Parent]`
6. Resolver uses `track.AuthorId` to fetch Author
7. Returns Author object
8. GraphQL extracts `name` field

**Benefits:**
- **Lazy loading**: Only fetch author if client requests it
- **Flexibility**: Can fetch from different source than Track
- **Batching**: Easy to optimize with DataLoaders

Field resolvers are fundamental to GraphQL's efficiency - clients get exactly the data they request, no more, no less."

---

### Q2: What is the N+1 problem and how does it occur? (Cumulative - builds on Stage 3)

**Answer:**
"The N+1 problem happens when fetching nested data results in N additional queries for N parent objects.

**Example:**

```graphql
query {
  tracksForHome {       # 10 tracks
    title
    author { name }     # Each track needs author
  }
}
```

**Without optimization:**
```
1. GetTracksForHome() → 1 query (returns 10 tracks)
2. For each track:
   GetAuthor(track.AuthorId) → 10 more queries
Total: 1 + 10 = 11 queries
```

**Why it's bad:**
- 100 tracks = 101 queries
- 1000 tracks = 1001 queries
- Scales linearly with data size
- Slow performance, high latency

**The solution: DataLoaders (Stage 6)**

DataLoaders batch and deduplicate:
```
1. GetTracksForHome() → 1 query (10 tracks)
2. Collect all author IDs: [cat-1, cat-2, cat-1, cat-3]
3. Deduplicate: [cat-1, cat-2, cat-3] (3 unique authors)
4. Batch fetch all 3 authors → 1 query
Total: 1 + 1 = 2 queries
```

**Result:** 11 queries → 2 queries (83% reduction!)

DataLoaders are essential for production GraphQL APIs to avoid the N+1 problem."

---

### Q3: Why do we use [Service] instead of constructor injection in resolvers?

**Answer:**
"Hot Chocolate uses the [Service] attribute for method-level dependency injection in resolvers, which has several advantages over constructor injection:

**Constructor Injection (Alternative):**
```csharp
public class Query
{
    private readonly ITrackService _service;
    public Query(ITrackService service) { _service = service; }

    public Task<List<Track>> GetTracksForHome()
        => _service.GetTracksForHomeAsync();
}
```

**Method-Level Injection ([Service]):**
```csharp
public class Query
{
    public Task<List<Track>> GetTracksForHome(
        [Service] ITrackService trackService)
        => trackService.GetTracksForHomeAsync();
}
```

**Advantages of [Service]:**

1. **Flexibility**: Different methods can use different services without adding all to constructor
2. **Clarity**: Immediately see which services each method needs
3. **Resolver chaining**: Works with [Parent] and other resolver attributes
4. **Hot Chocolate convention**: Integrates with framework patterns

**Both approaches work**, but [Service] is idiomatic Hot Chocolate and more flexible for complex resolvers with varying dependencies."

---

### Q4: How does GraphQL lazy loading improve performance compared to REST?

**Answer:**
"GraphQL's lazy loading with field resolvers eliminates over-fetching and under-fetching problems common in REST APIs.

**REST API Problem:**

```javascript
// REST endpoint: GET /api/track/c_0
// Always returns everything
{
  'id': 'c_0',
  'title': 'Catstronauts',
  'author': { ... },    // Fetched even if not needed
  'modules': [ ... ]   // Fetched even if not needed
}
```

**Three clients, three different needs:**
- Client 1: Just title and thumbnail (over-fetched author + modules)
- Client 2: Track + author (over-fetched modules)
- Client 3: Everything (perfect match, but only by chance)

**GraphQL Solution:**

```graphql
# Client 1: Just basic info
query { track { title thumbnail } }
# Only calls: GetTrack() → 1 API call

# Client 2: Track + author
query { track { title author { name } } }
# Calls: GetTrack(), GetAuthor() → 2 API calls

# Client 3: Everything
query { track { title author { name } modules { title } } }
# Calls: GetTrack(), GetAuthor(), GetModules() → 3 API calls
```

**Performance Impact:**

**REST:**
- Every request fetches all nested data
- Wastes bandwidth on unneeded data
- Can't customize per client

**GraphQL:**
- Only fetches requested fields
- Reduces bandwidth 30-70% typically
- Each client gets exactly what they need
- Single request instead of multiple REST calls

**Real numbers:** In a mobile app with limited bandwidth, fetching a track with GraphQL might be 5KB vs 20KB with REST. Over thousands of users, that's significant savings."

---

### Q5: Explain the [Parent] attribute and when you would use it. (GraphQL-focused)

**Answer:**
"The [Parent] attribute in Hot Chocolate injects the parent object from the previous resolver in the chain. It's essential for field resolvers.

**Use Case: Nested Data**

```graphql
query {
  track(id: 'c_0') {    # ← Root query
    title               # ← From Track object
    author { name }     # ← Need field resolver
  }
}
```

**Implementation:**

```csharp
// Root query - no parent
public async Task<Track?> GetTrack(
    string id,
    [Service] ITrackService service)
{
    return await service.GetTrackAsync(id);
    // Returns: Track { id: 'c_0', authorId: 'cat-1' }
}

// Field resolver - receives parent
[GraphQLName('author')]
public async Task<Author?> GetAuthor(
    [Parent] Track track,  // ← Hot Chocolate injects the Track from GetTrack
    [Service] ITrackService service)
{
    return await service.GetAuthorAsync(track.AuthorId);
    // Uses track.AuthorId to fetch the Author
}
```

**How it works:**
1. Client requests track with author field
2. GetTrack() executes, returns Track object
3. Hot Chocolate sees 'author' requested
4. Finds GetAuthor() resolver
5. Injects Track as [Parent] parameter
6. GetAuthor uses track.AuthorId to fetch data

**When to use [Parent]:**
- Field resolvers (resolving fields on a type)
- Nested data fetching
- When you need data from the parent object
- Computing derived fields

**Example of computed field:**
```csharp
[GraphQLName('fullDescription')]
public string GetFullDescription([Parent] Track track)
{
    return $'{track.Title} - {track.Description}';
    // Computes value from parent data
}
```

The [Parent] attribute is fundamental to GraphQL's nested data model - it connects resolvers in a chain to build complex response structures."

---

## Troubleshooting

### Issue 1: Field resolver not working

**Symptom:** Author field returns null or isn't in schema.

**Cause:** Missing [GraphQLName] attribute.

**Solution:**
```csharp
[GraphQLName("author")]  // ← Don't forget this!
public async Task<Author?> GetAuthor([Parent] Track track, ...)
```

---

### Issue 2: "Cannot resolve parameter" error

**Symptom:** Hot Chocolate error about resolving parameter.

**Cause:** Missing [Service] or [Parent] attribute.

**Solution:**
```csharp
// ❌ Wrong
public async Task<List<Track>> GetTracksForHome(ITrackService service)

// ✅ Right
public async Task<List<Track>> GetTracksForHome([Service] ITrackService service)
```

---

### Issue 3: Field shows in schema but always null

**Symptom:** Field appears in GraphQL Playground but returns null.

**Cause:** Field resolver exists but doesn't return data.

**Debug:**
1. Check if service method is actually called (add breakpoint/logging)
2. Verify service is registered in DI
3. Check if Track has required data (e.g., authorId not null)

---

## Summary

In Stage 4, we implemented **GraphQL queries** - the read operations of our API:

**What we created:**
- ✅ `tracksForHome` query - Fetch all tracks
- ✅ `track(id)` query - Fetch single track
- ✅ `author` field resolver - Lazy-load track authors
- ✅ `modules` field resolver - Lazy-load track modules
- ✅ 21 comprehensive tests

**Key learnings:**
- ✅ GraphQL queries are read-only operations
- ✅ Hot Chocolate maps methods to GraphQL fields (removes "Get", converts to camelCase)
- ✅ [Service] attribute injects dependencies from DI
- ✅ Field resolvers enable lazy loading and nested data
- ✅ [Parent] attribute provides parent object to field resolvers
- ✅ [GraphQLName] explicitly sets GraphQL field names
- ✅ N+1 problem occurs with field resolvers (solved in Stage 6)

**GraphQL powers unlocked:**
- ✅ Clients request exactly the data they need
- ✅ Single query for complex nested data
- ✅ Lazy loading (only fetch requested fields)
- ✅ No over-fetching or under-fetching

**Next up: Stage 5 - GraphQL Mutations** where we'll implement data modification operations with structured error handling!

---

**Interview Readiness:** You can now explain GraphQL queries, field resolvers, the [Parent] and [Service] attributes, lazy loading, the N+1 problem, and how GraphQL differs from REST. You're ready to discuss GraphQL architecture in depth! 🚀
