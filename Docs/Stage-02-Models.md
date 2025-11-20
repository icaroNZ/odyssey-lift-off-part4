# Stage 2: Data Models & DTOs

## Overview

In Stage 2, we created the **data models** (also called DTOs - Data Transfer Objects) that represent the structure of our GraphQL API. These are simple C# classes (POCOs - Plain Old C# Objects) that define what data our API can work with.

**What we built:**
- `Track` - Represents a learning track (course)
- `Author` - Represents a content creator
- `Module` - Represents a lesson within a track
- `IncrementTrackViewsResponse` - Represents the response from a mutation

**Why this matters:**
In GraphQL, your C# classes define your GraphQL schema. Hot Chocolate automatically generates the GraphQL types from these classes, so getting your models right is crucial for a clean, type-safe API.

---

## Learning Objectives

By the end of this stage, you will understand:

1. ✅ What POCOs (Plain Old C# Objects) are and why we use them
2. ✅ How C# classes map to GraphQL types
3. ✅ The difference between required and optional properties
4. ✅ How to use nullable reference types (`?`) in C#
5. ✅ The GraphQL mutation response pattern
6. ✅ How to write comprehensive unit tests for models
7. ✅ Why we separate data models from business logic

---

## Key Concepts

### Concept 1: POCOs (Plain Old C# Objects)

**What is a POCO?**

A POCO is a simple C# class that:
- Contains only properties (no methods with complex logic)
- Has no dependencies on frameworks or libraries
- Can be easily serialized/deserialized to JSON or other formats
- Focuses purely on **data structure**, not behavior

**Example:**
```csharp
public class Track
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
}
```

**Why POCOs?**
- ✅ **Simple**: Easy to understand, no complex logic
- ✅ **Testable**: No dependencies to mock
- ✅ **Serializable**: Works with JSON, GraphQL, databases
- ✅ **Flexible**: Can be used across layers (API, services, database)

**Interview Answer:**
> "POCOs are simple data containers with properties and minimal logic. We use them in GraphQL APIs because they're easy to serialize, test, and map to GraphQL types. They follow the Single Responsibility Principle - they're only responsible for holding data, not processing it."

---

### Concept 2: C# Properties to GraphQL Fields

**How Hot Chocolate Maps C# to GraphQL:**

**C# Model:**
```csharp
public class Track
{
    public string Id { get; set; }
    public string Title { get; set; }
    public int? Length { get; set; }
}
```

**Generated GraphQL Type:**
```graphql
type Track {
  id: String!      # Non-nullable (no ?)
  title: String!   # Non-nullable (no ?)
  length: Int      # Nullable (has ?)
}
```

**Key Mapping Rules:**
- `string` → `String!` (non-nullable by default in C# 11+)
- `string?` → `String` (nullable)
- `int` → `Int!` (non-nullable)
- `int?` → `Int` (nullable)
- `bool` → `Boolean!`
- `List<T>` → `[T!]!` (non-null list of non-null items)

**Interview Answer:**
> "Hot Chocolate uses reflection to inspect C# classes and generate GraphQL types. Properties become fields, and C# nullable reference types (`?`) map to GraphQL nullable fields. This gives us compile-time type safety in C# and runtime type safety in GraphQL."

---

### Concept 3: Nullable Reference Types

**What are Nullable Reference Types?**

In C# 8+, you can enable nullable reference types to make the compiler enforce null safety:

```csharp
// In .csproj
<Nullable>enable</Nullable>
```

**Before (C# 7 and earlier):**
```csharp
public class Track
{
    public string Title { get; set; }  // Can be null, compiler doesn't warn
}

// Later...
string title = track.Title;
int length = title.Length;  // CRASH! NullReferenceException if Title is null
```

**After (C# 8+ with nullable enabled):**
```csharp
public class Track
{
    public string Title { get; set; } = string.Empty;   // Non-nullable, must initialize
    public string? Thumbnail { get; set; }              // Nullable, can be null
}

// Later...
string title = track.Title;           // Safe, never null
int length = title.Length;            // Safe!

string? thumbnail = track.Thumbnail;
int thumbLength = thumbnail.Length;   // COMPILER WARNING! Might be null
```

**The `?` Operator:**
- `string?` - Can be null
- `int?` - Can be null (nullable value type)
- `Track?` - Can be null

**Why This Matters:**
- ✅ **Compile-time safety**: Catch null errors before runtime
- ✅ **Self-documenting**: Clear which fields can be null
- ✅ **GraphQL alignment**: Matches GraphQL's null/non-null system

**Interview Answer:**
> "Nullable reference types in C# 8+ make null handling explicit. By marking properties with `?`, we tell the compiler and other developers that a value can be null. This prevents the dreaded NullReferenceException at compile time rather than runtime, and it aligns perfectly with GraphQL's nullable field system."

---

### Concept 4: The Track Model

**Our Track class represents a learning track (course):**

```csharp
public class Track
{
    /// <summary>Unique identifier (e.g., "c_0", "c_1")</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Track title (e.g., "Catstronauts")</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>ID of the author (note: NOT an Author object!)</summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>URL to thumbnail image (optional)</summary>
    public string? Thumbnail { get; set; }

    /// <summary>Total length in minutes (optional)</summary>
    public int? Length { get; set; }

    /// <summary>Number of modules/lessons (optional)</summary>
    public int? ModulesCount { get; set; }

    /// <summary>Track description (optional)</summary>
    public string? Description { get; set; }

    /// <summary>View count (optional)</summary>
    public int? NumberOfViews { get; set; }
}
```

**Critical Design Decision: `AuthorId` vs `Author`**

```csharp
// ❌ DON'T do this:
public class Track
{
    public Author Author { get; set; }  // Tightly couples Track to Author
}

// ✅ DO this:
public class Track
{
    public string AuthorId { get; set; }  // Just store the ID
}
```

**Why store AuthorId instead of Author object?**

1. **GraphQL Field Resolvers**: We want Hot Chocolate to fetch the Author only when requested
2. **Lazy Loading**: Client might not need author info, why fetch it?
3. **Flexibility**: Makes it easy to implement DataLoaders (Stage 6)
4. **Separation**: Track data comes from one API call, Author from another

**Real-World Analogy:**

Think of a library catalog:
- The book card shows "Author: #12345" (AuthorId)
- If you want author details, you go to the author card catalog (field resolver)
- You don't carry the entire author biography on every book card!

**Interview Answer:**
> "We store AuthorId instead of an Author object to enable GraphQL field resolvers and lazy loading. This means when a client queries a track, we only fetch the author if they actually request it. It's more efficient and allows us to implement DataLoaders later to solve the N+1 problem."

---

### Concept 5: The Author Model

**Simple model representing a content creator:**

```csharp
public class Author
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Photo { get; set; }
}
```

**Why so simple?**
- In our example, authors only have basic info
- Real-world: Could have bio, social links, ratings, etc.
- Keep it simple for learning, easy to extend later

---

### Concept 6: The Module Model

**Represents a lesson within a track:**

```csharp
public class Module
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? Length { get; set; }
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
}
```

**One-to-Many Relationship:**
- One Track → Many Modules
- Track stores modules via `ModulesCount` property (count only)
- Actual modules fetched via field resolver (Stage 4)

---

### Concept 7: The GraphQL Mutation Response Pattern

**What is the Mutation Response Pattern?**

Instead of returning just the data, mutations should return a **response object** with:
- **code**: HTTP-style status code (200, 400, 404, 500)
- **success**: Boolean indicating success/failure
- **message**: Human-readable description
- **data**: The actual data (nullable)

**Our IncrementTrackViewsResponse:**

```csharp
public class IncrementTrackViewsResponse
{
    /// <summary>HTTP status code (200 = success, 4xx = client error, 5xx = server error)</summary>
    public int Code { get; set; }

    /// <summary>Was the operation successful?</summary>
    public bool Success { get; set; }

    /// <summary>Description of what happened (for user feedback)</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The updated track (null if operation failed)</summary>
    public Track? Track { get; set; }
}
```

**Why this pattern?**

**Without Response Pattern (Bad):**
```graphql
mutation {
  incrementTrackViews(id: "c_0") {
    id
    numberOfViews
  }
}
```
- ❌ What if track doesn't exist? Exception thrown, client gets generic error
- ❌ What if API is down? Client can't distinguish error types
- ❌ Client must use try-catch, no type-safe error handling

**With Response Pattern (Good):**
```graphql
mutation {
  incrementTrackViews(id: "c_0") {
    code         # 200, 404, 500, etc.
    success      # true/false
    message      # "Successfully incremented views" or "Track not found"
    track {      # The data (if successful)
      id
      numberOfViews
    }
  }
}
```
- ✅ Client can check `success` field
- ✅ Clear status codes like REST APIs
- ✅ User-friendly messages
- ✅ Type-safe error handling (no exceptions in GraphQL layer)

**Real-World Example:**

**Success Response:**
```json
{
  "incrementTrackViews": {
    "code": 200,
    "success": true,
    "message": "Successfully incremented views for track c_0",
    "track": {
      "id": "c_0",
      "numberOfViews": 101
    }
  }
}
```

**Error Response (Track Not Found):**
```json
{
  "incrementTrackViews": {
    "code": 404,
    "success": false,
    "message": "Track not found",
    "track": null
  }
}
```

**Interview Answer:**
> "The mutation response pattern wraps the result in an object with code, success, message, and data fields. This gives clients structured, predictable error handling without throwing exceptions. It's like REST API responses but in GraphQL - clients can check the success field and show appropriate UI based on the code and message."

---

## Step-by-Step Implementation

### Step 1: Create the Track Model

**File:** `Models/Track.cs`

```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents a learning track (course) in the Catstronauts platform.
/// Maps to the Track type in our GraphQL schema.
/// </summary>
public class Track
{
    /// <summary>
    /// Unique identifier for the track (e.g., "c_0", "c_1").
    /// Comes from the REST API.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The track's title/name.
    /// Example: "Catstronauts - Learn GraphQL"
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the author who created this track.
    /// Note: We store the ID, not the Author object itself.
    /// This enables GraphQL field resolvers and lazy loading.
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// URL to the track's thumbnail image.
    /// Nullable because not all tracks may have thumbnails.
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Total length of the track in minutes.
    /// Nullable because this might be calculated or unavailable.
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Number of modules (lessons) in this track.
    /// Nullable because this is metadata that might not always be available.
    /// </summary>
    public int? ModulesCount { get; set; }

    /// <summary>
    /// Detailed description of what the track covers.
    /// Nullable because not all tracks may have descriptions.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// How many times this track has been viewed.
    /// Nullable because new tracks might not have view counts yet.
    /// </summary>
    public int? NumberOfViews { get; set; }
}
```

**Key Points:**
- All properties are `public` (GraphQL needs to access them)
- `{ get; set; }` - Both getter and setter (mutable)
- `= string.Empty` - Initialize non-nullable strings
- `?` - Marks optional properties
- XML doc comments - Explain what each property means

---

### Step 2: Create the Author Model

**File:** `Models/Author.cs`

```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents an author (content creator) in the Catstronauts platform.
/// Authors create tracks (courses).
/// </summary>
public class Author
{
    /// <summary>
    /// Unique identifier for the author (e.g., "cat-1", "cat-2").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The author's display name.
    /// Example: "Henri the Cat"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL to the author's profile photo.
    /// Nullable because some authors might not have photos.
    /// </summary>
    public string? Photo { get; set; }
}
```

---

### Step 3: Create the Module Model

**File:** `Models/Module.cs`

```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Represents a module (lesson) within a track.
/// Tracks are composed of multiple modules.
/// </summary>
public class Module
{
    /// <summary>
    /// Unique identifier for the module (e.g., "l_0", "l_1").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The module's title.
    /// Example: "Introduction to GraphQL"
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Length of the module in minutes.
    /// Nullable because this might not always be available.
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Text content/description of what this module covers.
    /// Nullable because some modules might be video-only.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// URL to the module's video content.
    /// Nullable because some modules might be text-only.
    /// </summary>
    public string? VideoUrl { get; set; }
}
```

---

### Step 4: Create the Mutation Response Model

**File:** `Models/IncrementTrackViewsResponse.cs`

```csharp
namespace Catstronauts.GraphQL.Models;

/// <summary>
/// Response type for the incrementTrackViews mutation.
/// Follows the GraphQL mutation response pattern with code, success, message, and data.
/// </summary>
/// <remarks>
/// This pattern provides structured error handling for clients:
/// - code: HTTP-style status code (200, 404, 500, etc.)
/// - success: Quick boolean check for success/failure
/// - message: Human-readable feedback for the user
/// - track: The updated data (null on error)
///
/// Example success response:
/// {
///   code: 200,
///   success: true,
///   message: "Successfully incremented views for track c_0",
///   track: { id: "c_0", numberOfViews: 101 }
/// }
///
/// Example error response:
/// {
///   code: 404,
///   success: false,
///   message: "Track not found",
///   track: null
/// }
/// </remarks>
public class IncrementTrackViewsResponse
{
    /// <summary>
    /// HTTP status code indicating the result.
    /// - 200: Success
    /// - 400: Bad request (invalid input)
    /// - 404: Track not found
    /// - 500: Server error
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Indicates whether the mutation succeeded.
    /// Convenient for client-side conditional logic.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message describing the result.
    /// Can be shown to users in the UI.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The updated track with new view count.
    /// Null if the mutation failed.
    /// </summary>
    public Track? Track { get; set; }
}
```

---

### Step 5: Write Comprehensive Tests

**File:** `Catstronauts.Tests/Models/ModelTests.cs`

We wrote **18 tests** covering:

1. **Default Instantiation** (4 tests) - Can we create instances?
2. **Full Instantiation** (4 tests) - Can we set all properties?
3. **Required vs Optional** (3 tests) - Are nullability rules correct?
4. **Response Scenarios** (3 tests) - Success/error response handling
5. **Property Behavior** (4 tests) - Can we modify properties?

**Example Tests:**

```csharp
[Fact]
public void Track_DefaultInstantiation_CreatesInstance()
{
    // Arrange & Act
    var track = new Track();

    // Assert
    track.Should().NotBeNull();
    track.Id.Should().Be(string.Empty);  // Default value
    track.Title.Should().Be(string.Empty);
    track.AuthorId.Should().Be(string.Empty);
}

[Fact]
public void Track_FullInstantiation_SetsAllProperties()
{
    // Arrange & Act
    var track = new Track
    {
        Id = "c_0",
        Title = "Catstronauts",
        AuthorId = "cat-1",
        Thumbnail = "https://example.com/thumb.jpg",
        Length = 120,
        ModulesCount = 6,
        Description = "Learn GraphQL with cats!",
        NumberOfViews = 1337
    };

    // Assert
    track.Id.Should().Be("c_0");
    track.Title.Should().Be("Catstronauts");
    track.NumberOfViews.Should().Be(1337);
}

[Fact]
public void Track_OptionalProperties_CanBeNull()
{
    // Arrange & Act
    var track = new Track
    {
        Id = "c_0",
        Title = "Test",
        AuthorId = "cat-1",
        // Optional properties not set
    };

    // Assert
    track.Thumbnail.Should().BeNull();
    track.Length.Should().BeNull();
    track.Description.Should().BeNull();
}

[Fact]
public void IncrementTrackViewsResponse_SuccessScenario_HasCorrectStructure()
{
    // Arrange & Act
    var response = new IncrementTrackViewsResponse
    {
        Code = 200,
        Success = true,
        Message = "Views incremented successfully",
        Track = new Track { Id = "c_0", NumberOfViews = 101 }
    };

    // Assert
    response.Code.Should().Be(200);
    response.Success.Should().BeTrue();
    response.Message.Should().NotBeNullOrEmpty();
    response.Track.Should().NotBeNull();
}

[Fact]
public void IncrementTrackViewsResponse_ErrorScenario_HasCorrectStructure()
{
    // Arrange & Act
    var response = new IncrementTrackViewsResponse
    {
        Code = 404,
        Success = false,
        Message = "Track not found",
        Track = null
    };

    // Assert
    response.Code.Should().Be(404);
    response.Success.Should().BeFalse();
    response.Message.Should().Contain("not found");
    response.Track.Should().BeNull();
}
```

**Why Test Models?**

You might think: "These are just properties, why test them?"

**Good reasons:**
- ✅ **Verify nullability**: Ensure optional properties can be null
- ✅ **Catch typos**: Property name misspellings break at compile time
- ✅ **Document intent**: Tests show how to use the models
- ✅ **Prevent regressions**: If someone changes a property, tests fail
- ✅ **Design feedback**: If tests are hard to write, design might be wrong

---

## Design Decisions & Trade-offs

### Decision 1: POCOs vs Rich Domain Models

**What we chose:** POCOs (simple data containers)

**Alternative:** Rich domain models with behavior

```csharp
// Rich domain model (we didn't do this)
public class Track
{
    public string Id { get; private set; }  // Private setter

    public void IncrementViews()  // Business logic in model
    {
        if (NumberOfViews == null)
            NumberOfViews = 0;

        NumberOfViews++;
    }
}
```

**Why POCOs?**
- ✅ Simpler for GraphQL APIs (focus on data transfer)
- ✅ Easier to serialize/deserialize
- ✅ Business logic lives in services (separation of concerns)
- ✅ Matches our architecture (REST API client)

**When to use Rich Models:**
- Complex business rules
- Domain-Driven Design (DDD)
- Need validation logic
- Working directly with databases

---

### Decision 2: `AuthorId` vs `Author` Object

**What we chose:** Store `AuthorId` (string)

**Alternative:** Store full `Author` object

**Why AuthorId?**
- ✅ Enables GraphQL field resolvers (lazy loading)
- ✅ More flexible (can fetch author from different sources)
- ✅ Supports DataLoaders (Stage 6)
- ✅ Matches REST API structure

**Trade-off:**
- ❌ Need extra code to fetch author (but GraphQL does this anyway)

---

### Decision 3: Nullable vs Non-Nullable Properties

**How we decided:**

**Required (non-nullable):**
- `Id`, `Title`, `AuthorId` - Core identity fields
- `Name` - Essential for display

**Optional (nullable):**
- `Thumbnail`, `Photo` - Media might not exist
- `Length`, `ModulesCount` - Calculated/aggregated data
- `Description`, `Content` - Not all items have these
- `VideoUrl` - Text-only content possible
- `NumberOfViews` - New items might not have views

**Guideline:** If the field is essential for the object's identity or always present in the API, make it non-nullable. Otherwise, nullable.

---

### Decision 4: Mutation Response Pattern

**What we chose:** Structured response with code/success/message/data

**Alternative:** Throw exceptions or return data directly

**Why structured responses?**
- ✅ Type-safe error handling (no exceptions in GraphQL layer)
- ✅ Clear status codes
- ✅ User-friendly messages
- ✅ Partial success possible (if needed)

**Trade-off:**
- ❌ More verbose (but worth it for robust error handling)

---

## Interview Preparation

### Q1: What's the difference between a POCO and a DTO?

**Answer:**
"POCO (Plain Old C# Object) and DTO (Data Transfer Object) are often used interchangeably, but there's a subtle difference:

**POCO** is a general term for any simple C# class without framework dependencies. It could be used anywhere - domain models, DTOs, view models, etc.

**DTO** specifically refers to objects designed to transfer data between layers or systems. In our GraphQL API, our models are technically DTOs because they transfer data between the REST API and our GraphQL layer.

In practice, the terms overlap significantly. What matters is the pattern: simple objects with properties, no business logic, easy to serialize."

---

### Q2: Why not just use `dynamic` or `Dictionary<string, object>` instead of creating classes?

**Answer:**
"Using strongly-typed classes instead of `dynamic` or dictionaries provides several critical benefits:

**1. Compile-time safety:** Typos in property names are caught at compile time, not runtime.

**2. IntelliSense:** IDEs can autocomplete properties, making development faster.

**3. GraphQL schema generation:** Hot Chocolate uses reflection on classes to generate GraphQL types. With `dynamic`, it wouldn't know what fields to expose.

**4. Refactoring:** Renaming a property updates all usages automatically. With dictionaries, you'd have to find all string keys manually.

**5. Documentation:** XML comments on properties document your API. Dictionaries have no such mechanism.

**6. Performance:** Classes are more memory-efficient than dictionaries and have better performance.

The only time I'd use `dynamic` or dictionaries is for truly schema-less data, like user-defined custom fields."

---

### Q3: Explain nullable reference types in C# and how they relate to GraphQL's null handling.

**Answer:**
"Nullable reference types, introduced in C# 8, make null handling explicit at compile time.

**Without nullable reference types:**
```csharp
string title = track.Title;  // Might be null, compiler doesn't warn
int length = title.Length;   // Runtime crash if null
```

**With nullable reference types:**
```csharp
string title = track.Title;   // Compiler ensures this is never null
string? thumbnail = track.Thumbnail;  // Compiler knows this might be null
int length = thumbnail.Length;  // COMPILER WARNING!
```

**GraphQL Connection:**

In GraphQL, you explicitly mark fields as nullable or non-nullable:
```graphql
type Track {
  id: String!          # Non-nullable (! = required)
  title: String!       # Non-nullable
  thumbnail: String    # Nullable (no !)
}
```

Hot Chocolate maps C# nullability to GraphQL nullability:
- `string` → `String!` (non-nullable)
- `string?` → `String` (nullable)

This gives us **two layers of protection**:
1. Compile-time in C# (nullable reference types)
2. Runtime in GraphQL (type validation)

It's like having both a seatbelt and an airbag - multiple safety mechanisms."

---

### Q4: What is the mutation response pattern and why is it important in GraphQL?

**Answer:**
"The mutation response pattern is a best practice where mutations return a structured response object instead of just the data or throwing exceptions.

**Structure:**
```csharp
{
  code: Int!       // HTTP-style status code
  success: Boolean! // Quick success/failure check
  message: String!  // Human-readable description
  data: Type       // The actual data (nullable)
}
```

**Why it's important:**

**1. Structured error handling:** Instead of throwing exceptions, mutations return error information in a predictable format. Clients can check the `success` field and handle errors gracefully.

**2. User-friendly feedback:** The `message` field provides text you can show to users: 'Successfully updated profile' or 'Email already in use.'

**3. HTTP semantics in GraphQL:** The `code` field provides familiar HTTP status codes (200, 400, 404, 500), making it easier to reason about errors.

**4. Partial success:** In complex mutations affecting multiple resources, you can return success=true with warnings in the message.

**5. Type safety:** Clients know exactly what fields are available in responses and errors, unlike exception-based approaches where error structures vary.

**Example:**
```graphql
mutation {
  incrementTrackViews(id: 'c_0') {
    success
    message
    track { numberOfViews }
  }
}
```

If successful, `success=true` and `track` has data. If failed (e.g., track not found), `success=false`, `track=null`, and `message` explains why. No exceptions thrown, no ambiguity."

---

### Q5: How do you decide which properties should be nullable vs non-nullable?

**Answer:**
"I use this decision framework:

**Make it NON-NULLABLE (required) if:**
- It's part of the object's core identity (like `Id`)
- It's always present in the source data
- The object doesn't make sense without it (like `Title` for a Track)
- It's a required field in the database schema

**Make it NULLABLE (optional) if:**
- It's optional in the source data (API, database)
- It's calculated or aggregated data that might not always be available
- It's media content (images, videos) that might not exist
- It's metadata that's not essential (like `Description`)
- New records might not have it yet (like `NumberOfViews` for a brand new track)

**Real example from our Track model:**
- `Id`, `Title`, `AuthorId` → Non-nullable (core identity)
- `Thumbnail`, `Description` → Nullable (not always present)
- `Length`, `ModulesCount` → Nullable (calculated data)
- `NumberOfViews` → Nullable (new tracks have no views)

**GraphQL benefit:** This nullability flows through to the GraphQL schema, giving clients clear expectations about which fields they must handle as potentially null."

---

### Q6: Can you explain the relationship between Track, Author, and Module models?

**Answer:**
"Our models have these relationships:

**Track → Author (Many-to-One):**
- Many tracks can be created by one author
- Track stores `AuthorId` (foreign key)
- We don't store the full Author object in Track
- GraphQL field resolver fetches the Author when requested

**Track → Modules (One-to-Many):**
- One track contains many modules (lessons)
- Track stores `ModulesCount` (aggregate)
- Individual modules fetched via field resolver
- Modules don't store a TrackId in our model (API provides this relationship)

**Why we store IDs instead of objects:**

**1. GraphQL field resolvers:** We want lazy loading - only fetch related data when the client requests it.

**2. Separation of concerns:** Track data comes from one API endpoint, Author from another.

**3. Flexibility:** Easy to implement caching, DataLoaders, or fetch from different sources.

**4. Avoids circular references:** If Track contained Author, and Author contained List<Track>, we'd have infinite loops during serialization.

**GraphQL makes this seamless:**
```graphql
query {
  track(id: 'c_0') {
    title
    author { name }    # Resolved separately
    modules { title }  # Resolved separately
  }
}
```

Behind the scenes:
1. Fetch track (includes authorId)
2. If client requested author, resolve it using authorId
3. If client requested modules, resolve them using track id

This is the power of GraphQL - relationships are defined in resolvers, not in the data models."

---

## Troubleshooting

### Issue 1: "Cannot convert null to 'string' because it is a non-nullable type"

**Symptom:**
```csharp
var track = new Track();
track.Title = null;  // COMPILER ERROR
```

**Cause:** Nullable reference types are enabled, and `Title` is non-nullable.

**Solution:**
Either initialize the property:
```csharp
var track = new Track
{
    Title = "My Track"  // Provide a value
};
```

Or change the property to nullable:
```csharp
public string? Title { get; set; }  // Now can be null
```

---

### Issue 2: "Property 'Track.Author' not found in GraphQL schema"

**Symptom:** You have `public Author Author { get; set; }` but GraphQL queries fail.

**Cause:** Hot Chocolate sees the `Author` property but doesn't know how to resolve it.

**Solution:**
1. Either remove the `Author` property and use `AuthorId`
2. Or create a field resolver (covered in Stage 4)

---

### Issue 3: Model properties not showing in GraphQL schema

**Symptom:** You added a property but it doesn't appear in GraphQL Playground.

**Checklist:**
- ✅ Property is `public`?
- ✅ Property has both `{ get; set; }`?
- ✅ Class is `public`?
- ✅ Restarted the server?
- ✅ Cleared GraphQL Playground cache?

---

### Issue 4: Tests failing with "expected X but was null"

**Symptom:**
```csharp
track.Title.Should().Be("Test");  // Expected "Test" but was ""
```

**Cause:** You initialized with `= string.Empty` but forgot to set the value.

**Solution:**
```csharp
var track = new Track
{
    Title = "Test"  // ← Don't forget to set it!
};
```

---

## Summary

In Stage 2, we built the foundation of our GraphQL API by creating **data models** (POCOs):

**What we created:**
- ✅ `Track` - Learning track with 8 properties
- ✅ `Author` - Content creator with 3 properties
- ✅ `Module` - Lesson with 5 properties
- ✅ `IncrementTrackViewsResponse` - Mutation response with code/success/message/data

**Key learnings:**
- ✅ POCOs are simple data containers, perfect for GraphQL APIs
- ✅ C# classes map to GraphQL types automatically
- ✅ Nullable reference types (`?`) provide compile-time null safety
- ✅ Store IDs instead of objects to enable field resolvers
- ✅ Mutation response pattern provides structured error handling
- ✅ Comprehensive tests ensure models work correctly

**Design principles applied:**
- Single Responsibility: Models only hold data, no business logic
- Explicit nullability: Clear which properties can be null
- Separation of concerns: Relationships via IDs, not nested objects
- Type safety: Strong typing prevents runtime errors

**GraphQL connection:**
These models define our GraphQL schema. In Stage 4, we'll create **queries** and **field resolvers** that use these models to fetch and return data.

**Next up: Stage 3 - Service Layer** where we'll create the `TrackService` to fetch data from the REST API, bringing these models to life with real data!

---

**Interview Readiness:** You can now explain POCOs, nullable reference types, GraphQL type mapping, the mutation response pattern, and why we design models the way we do. You're ready to discuss data modeling decisions in a GraphQL API interview! 🚀
