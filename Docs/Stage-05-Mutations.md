# Stage 5: GraphQL Mutations

## Overview

In Stage 5, we implemented **GraphQL mutations** - operations that modify data on the server. We created the `incrementTrackViews` mutation with structured error handling following GraphQL best practices.

**What we built:**
- `Mutation` class with `IncrementTrackViews` resolver
- Mutation response pattern (code, success, message, data)
- Structured error handling (no exceptions thrown to GraphQL)
- Registered mutation type in DI
- 25 comprehensive tests covering all scenarios

**Why this matters:**
Mutations are how clients modify data in GraphQL. Proper error handling and response patterns make APIs robust, user-friendly, and type-safe.

---

## Learning Objectives

By the end of this stage, you will understand:

1. ✅ GraphQL mutations vs queries
2. ✅ The mutation response pattern (code/success/message/data)
3. ✅ Why we return responses instead of throwing exceptions
4. ✅ Structured error handling in GraphQL
5. ✅ HTTP status codes in mutation responses
6. ✅ Sequential execution of mutations
7. ✅ Testing mutation resolvers comprehensively
8. ✅ How to provide user-friendly error messages

---

## Key Concepts

### Concept 1: Mutations vs Queries (Deep Dive)

**The Fundamental Difference:**

| Aspect | Queries | Mutations |
|--------|---------|-----------|
| **Purpose** | Read data | Modify data |
| **Side Effects** | None (idempotent) | Changes server state |
| **Execution** | Can run in parallel | Run sequentially |
| **Convention** | Start with query { ... } | Start with mutation { ... } |
| **Caching** | Cacheable | Not cacheable |

**Sequential vs Parallel Execution:**

**Queries (Parallel):**
```graphql
query {
  user1: user(id: "1") { name }  # Executes
  user2: user(id: "2") { name }  # Executes  } in parallel
  user3: user(id: "3") { name }  # Executes
}
```
Order doesn't matter - all queries can run simultaneously.

**Mutations (Sequential):**
```graphql
mutation {
  first: incrementTrackViews(id: "c_0") { success }   # Executes first
  second: incrementTrackViews(id: "c_1") { success }  # Then this
  third: incrementTrackViews(id: "c_2") { success }   # Then this
}
```
**GraphQL guarantee:** Mutations execute in the order they appear.

**Why sequential?**
```graphql
mutation {
  createUser(name: "John") { id }       # Step 1: Create user
  createPost(userId: "...", ...) { }    # Step 2: Needs user from step 1
}
```
Subsequent mutations might depend on earlier ones.

**Interview Answer:**
> "Mutations modify server state and run sequentially to ensure correct ordering. Queries read data without side effects and can run in parallel for performance. This distinction is enforced by GraphQL - if you send multiple mutations in one request, they execute one at a time in order, but multiple queries can execute simultaneously."

---

### Concept 2: The Mutation Response Pattern

**Why Not Just Return the Data?**

**Bad (Just return data):**
```graphql
mutation {
  incrementTrackViews(id: "c_0") {
    id
    numberOfViews  # What if track doesn't exist? Exception thrown!
  }
}
```

**Problems:**
- ❌ No way to communicate errors without exceptions
- ❌ Client can't distinguish error types
- ❌ No user-friendly error messages
- ❌ Forces try-catch everywhere

**Good (Structured Response):**
```graphql
mutation {
  incrementTrackViews(id: "c_0") {
    code         # HTTP status code (200, 404, 500)
    success      # true/false
    message      # "Successfully incremented views" or "Track not found"
    track {      # The data (null on error)
      id
      numberOfViews
    }
  }
}
```

**Response Type:**
```csharp
public class IncrementTrackViewsResponse
{
    public int Code { get; set; }          // 200, 400, 404, 500, etc.
    public bool Success { get; set; }      // true/false
    public string Message { get; set; }    // User-friendly description
    public Track? Track { get; set; }      // Data (null on error)
}
```

**Example Responses:**

**Success:**
```json
{
  "incrementTrackViews": {
    "code": 200,
    "success": true,
    "message": "Successfully incremented views for track c_0.",
    "track": {
      "id": "c_0",
      "numberOfViews": 101
    }
  }
}
```

**Error (Track Not Found):**
```json
{
  "incrementTrackViews": {
    "code": 404,
    "success": false,
    "message": "Failed to increment views: Track not found",
    "track": null
  }
}
```

**Error (Validation):**
```json
{
  "incrementTrackViews": {
    "code": 400,
    "success": false,
    "message": "Track ID cannot be null or empty.",
    "track": null
  }
}
```

**Interview Answer:**
> "The mutation response pattern wraps results in a structured object with code, success, message, and data fields. This provides type-safe error handling without exceptions. The `success` field lets clients quickly check if it worked, `code` provides HTTP-style status codes for different scenarios, `message` gives user-friendly text to display, and `track` contains the data if successful. It's like REST API responses but in GraphQL."

---

### Concept 3: Structured Error Handling (No Exceptions to GraphQL)

**The Strategy:**

Instead of throwing exceptions (which become GraphQL errors), return error information in the response.

**Our Implementation:**

```csharp
public async Task<IncrementTrackViewsResponse> IncrementTrackViews(
    string id,
    [Service] ITrackService trackService)
{
    // Level 1: Validation Errors (400)
    if (string.IsNullOrWhiteSpace(id))
    {
        return new IncrementTrackViewsResponse
        {
            Code = 400,
            Success = false,
            Message = "Track ID cannot be null or empty.",
            Track = null
        };
    }

    try
    {
        // Level 2: Success (200)
        var track = await trackService.IncrementTrackViewsAsync(id);

        return new IncrementTrackViewsResponse
        {
            Code = 200,
            Success = true,
            Message = $"Successfully incremented views for track {id}.",
            Track = track
        };
    }
    catch (HttpRequestException ex)
    {
        // Level 3: HTTP Errors (404, 500, etc.)
        var statusCode = ex.StatusCode.HasValue
            ? (int)ex.StatusCode.Value
            : 500;

        return new IncrementTrackViewsResponse
        {
            Code = statusCode,
            Success = false,
            Message = $"Failed to increment views: {ex.Message}",
            Track = null
        };
    }
    catch (Exception ex)
    {
        // Level 4: Unexpected Errors (500)
        return new IncrementTrackViewsResponse
        {
            Code = 500,
            Success = false,
            Message = $"An unexpected error occurred: {ex.Message}",
            Track = null
        };
    }
}
```

**Three Levels of Error Handling:**

1. **Validation** (before service call): Invalid input → 400
2. **HTTP Errors** (from service): API errors → 404, 500, etc.
3. **Unexpected Errors** (catch-all): Unknown issues → 500

**Why This Approach?**

**Without structured handling:**
```csharp
public async Task<Track> IncrementTrackViews(string id, ...)
{
    if (string.IsNullOrWhiteSpace(id))
        throw new ArgumentException("Invalid ID");  // ❌ Exception!

    return await trackService.IncrementTrackViewsAsync(id);  // ❌ Exception!
}
```

**GraphQL Response:**
```json
{
  "data": null,
  "errors": [{
    "message": "Invalid ID",  // Generic error structure
    "path": ["incrementTrackViews"]
  }]
}
```

**Problems:**
- ❌ Client gets `data: null` - can't access partial data
- ❌ Error structure varies (not type-safe)
- ❌ Can't show user-friendly messages
- ❌ No status codes

**With structured handling:**
```json
{
  "data": {
    "incrementTrackViews": {
      "code": 400,
      "success": false,
      "message": "Track ID cannot be null or empty.",
      "track": null
    }
  }
}
```

**Benefits:**
- ✅ Consistent response structure
- ✅ Type-safe (client knows all fields)
- ✅ User-friendly messages
- ✅ Status codes for different handling
- ✅ No exceptions in GraphQL layer

**Interview Answer:**
> "We use structured error handling by returning error information in the response instead of throwing exceptions. This gives clients predictable, type-safe responses. Whether the mutation succeeds or fails, the response always has code, success, message, and data fields. Clients can check `success` and show `message` to users, rather than parsing exception messages. It's more robust and user-friendly than exception-based error handling."

---

### Concept 4: HTTP Status Codes in GraphQL

**Using Familiar HTTP Semantics:**

Even though GraphQL uses POST requests, we can use HTTP status codes in responses for familiarity:

| Code | Meaning | Example |
|------|---------|---------|
| **200** | Success | Successfully incremented views |
| **400** | Bad Request | Invalid input (null ID, wrong format) |
| **404** | Not Found | Track doesn't exist |
| **500** | Internal Server Error | API down, network error |
| **503** | Service Unavailable | API temporarily unavailable |

**Why HTTP Codes in GraphQL?**

1. **Familiar**: Developers know HTTP codes
2. **Categorization**: Easy to group errors (4xx = client error, 5xx = server error)
3. **Conditional logic**: `if (code >= 400 && code < 500)` → client problem
4. **REST API passthrough**: Preserve codes from upstream API

**Example Usage:**

```csharp
// Extract status code from HttpRequestException
catch (HttpRequestException ex)
{
    var statusCode = ex.StatusCode.HasValue
        ? (int)ex.StatusCode.Value  // Use actual status code
        : 500;                       // Default to 500 if unknown

    return new IncrementTrackViewsResponse
    {
        Code = statusCode,  // 404, 500, 503, etc.
        ...
    };
}
```

**Client-Side Handling:**

```javascript
const result = await incrementTrackViews({ id: 'c_0' });

if (result.success) {
  // Show success message
  showToast(result.message);
} else {
  if (result.code === 404) {
    // Track not found - show different UI
    showError('Track not found. It may have been deleted.');
  } else if (result.code >= 500) {
    // Server error - suggest retry
    showError('Server error. Please try again.');
  } else {
    // Other errors
    showError(result.message);
  }
}
```

---

### Concept 5: Testing Mutations Comprehensively

**Our 25 Tests Cover:**

1. **Success Scenarios (3 tests)**
   - Valid ID → 200 success
   - All track properties returned
   - Correct service method called

2. **Validation Errors (3 tests)**
   - Null ID → 400
   - Empty string → 400
   - Whitespace → 400
   - **Critical**: Service NOT called for invalid input

3. **HTTP Errors (4 tests)**
   - 404 Not Found → Returns 404
   - 500 Internal Server Error → Returns 500
   - 503 Service Unavailable → Returns 503
   - No status code → Defaults to 500

4. **Unexpected Errors (2 tests)**
   - Generic exception → 500
   - NullReferenceException → 500

5. **Response Structure (2 tests)**
   - Success response has all fields
   - Error response has all fields

6. **Type & Method Verification (3 tests)**
   - Method is async
   - Correct parameters
   - Returns correct type

7. **Edge Cases & Business Logic (5 tests)**
   - Long IDs still work
   - Success message includes track ID
   - Error message includes original error
   - Multiple error scenarios

**Example Test - Validation:**
```csharp
[Fact]
public async Task IncrementTrackViews_WithNullId_Returns400Error()
{
    // Arrange
    var mockService = Substitute.For<ITrackService>();
    var mutation = new Mutation();

    // Act
    var response = await mutation.IncrementTrackViews(null!, mockService);

    // Assert
    response.Success.Should().BeFalse();
    response.Code.Should().Be(400);
    response.Message.Should().Contain("Track ID cannot be null or empty");
    response.Track.Should().BeNull();

    // CRITICAL: Service should NOT be called for invalid input
    await mockService.DidNotReceive().IncrementTrackViewsAsync(Arg.Any<string>());
}
```

**Example Test - Error Propagation:**
```csharp
[Fact]
public async Task IncrementTrackViews_WhenApiReturns404_Returns404Error()
{
    // Arrange
    var mockService = Substitute.For<ITrackService>();
    var httpException = new HttpRequestException(
        "Track not found",
        null,
        HttpStatusCode.NotFound);
    mockService.IncrementTrackViewsAsync("nonexistent")
        .ThrowsAsync(httpException);
    var mutation = new Mutation();

    // Act
    var response = await mutation.IncrementTrackViews("nonexistent", mockService);

    // Assert
    response.Code.Should().Be(404);  // Preserves status code
    response.Success.Should().BeFalse();
    response.Message.Should().Contain("Track not found");
    response.Track.Should().BeNull();
}
```

---

## Step-by-Step Implementation

### Step 1: Create Mutation Class

**File:** `GraphQL/Mutations/Mutation.cs`

```csharp
using Catstronauts.GraphQL.Models;
using Catstronauts.GraphQL.Services;

namespace Catstronauts.GraphQL.GraphQL.Mutations;

public class Mutation
{
    public async Task<IncrementTrackViewsResponse> IncrementTrackViews(
        string id,
        [Service] ITrackService trackService)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(id))
        {
            return new IncrementTrackViewsResponse
            {
                Code = 400,
                Success = false,
                Message = "Track ID cannot be null or empty.",
                Track = null
            };
        }

        try
        {
            // Success path
            var track = await trackService.IncrementTrackViewsAsync(id);

            return new IncrementTrackViewsResponse
            {
                Code = 200,
                Success = true,
                Message = $"Successfully incremented views for track {id}.",
                Track = track
            };
        }
        catch (HttpRequestException ex)
        {
            // HTTP errors from API
            var statusCode = ex.StatusCode.HasValue
                ? (int)ex.StatusCode.Value
                : 500;

            return new IncrementTrackViewsResponse
            {
                Code = statusCode,
                Success = false,
                Message = $"Failed to increment views: {ex.Message}",
                Track = null
            };
        }
        catch (Exception ex)
        {
            // Unexpected errors
            return new IncrementTrackViewsResponse
            {
                Code = 500,
                Success = false,
                Message = $"An unexpected error occurred: {ex.Message}",
                Track = null
            };
        }
    }
}
```

### Step 2: Register Mutation in DI

**File:** `Program.cs`

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();  // ← Add this line
```

**Generated GraphQL Schema:**
```graphql
type Mutation {
  incrementTrackViews(id: String!): IncrementTrackViewsResponse!
}

type IncrementTrackViewsResponse {
  code: Int!
  success: Boolean!
  message: String!
  track: Track
}
```

---

## Design Decisions & Trade-offs

### Decision 1: Structured Responses vs Exceptions

**What we chose:** Structured responses with code/success/message/data

**Alternative:** Throw exceptions

**Why structured responses?**
- ✅ Type-safe error handling
- ✅ User-friendly messages
- ✅ Consistent response structure
- ✅ No GraphQL errors array

**Trade-off:**
- ❌ More verbose (but worth it)

---

### Decision 2: HTTP Status Codes in Responses

**What we chose:** Include familiar HTTP codes (200, 400, 404, 500)

**Alternative:** Custom error codes or just success boolean

**Why HTTP codes?**
- ✅ Familiar to developers
- ✅ Easy categorization (4xx vs 5xx)
- ✅ Passthrough from REST API

---

## Interview Preparation

### Q1: Why return a response type instead of just the Track? (GraphQL-focused)

**Answer:**
"Returning a structured response type provides critical information that just returning data doesn't:

**Just data approach:**
```csharp
public async Task<Track> IncrementTrackViews(string id, ...)
{
    return await trackService.IncrementTrackViewsAsync(id);
    // If it fails, throws exception → GraphQL error
}
```

**Response type approach:**
```csharp
public async Task<IncrementTrackViewsResponse> IncrementTrackViews(string id, ...)
{
    try {
        var track = await trackService.IncrementTrackViewsAsync(id);
        return new IncrementTrackViewsResponse {
            Code = 200,
            Success = true,
            Message = 'Success!',
            Track = track
        };
    } catch (Exception ex) {
        return new IncrementTrackViewsResponse {
            Code = 500,
            Success = false,
            Message = ex.Message,
            Track = null
        };
    }
}
```

**Benefits:**
1. **Type-safe error handling**: Client knows response structure for success AND errors
2. **User-friendly**: `message` field can be shown directly to users
3. **Status codes**: Different handling for different error types (404 vs 500)
4. **No exceptions**: GraphQL data is always present, not in errors array
5. **Partial success**: Could return success=true with warnings in message

Real-world example: When incrementing views fails because track doesn't exist, client sees:
```json
{
  'success': false,
  'message': 'Track not found',
  'code': 404
}
```
And can show a helpful 'Track may have been deleted' message instead of generic 'An error occurred'."

---

### Q2: How do mutations differ from queries in GraphQL? (Cumulative)

**Answer:**
"Mutations and queries have three fundamental differences:

**1. Purpose:**
- Queries: Read data (no side effects)
- Mutations: Modify data (changes server state)

**2. Execution:**
- Queries: Can run in parallel (order doesn't matter)
- Mutations: Run sequentially (order guaranteed)

**3. Response pattern:**
- Queries: Often return data directly
- Mutations: Should return response types with status/message

**Example:**
```graphql
query {
  user1: user(id: '1') { name }  # These can
  user2: user(id: '2') { name }  # run in
  user3: user(id: '3') { name }  # parallel
}

mutation {
  first: createUser(name: 'John') { id }    # Runs first
  second: createPost(userId: '...') { id }  # Then this (may need result from first)
}
```

GraphQL enforces sequential mutation execution because later mutations might depend on earlier ones. Queries have no dependencies (idempotent reads), so they can safely run in parallel for performance.

This distinction is enforced by the GraphQL spec - implementations must run mutations sequentially."

---

### Q3: Explain your error handling strategy in mutations. (Cumulative - builds on Stage 3)

**Answer:**
"We use a three-level error handling strategy that never throws exceptions to GraphQL:

**Level 1: Validation (400 Bad Request)**
```csharp
if (string.IsNullOrWhiteSpace(id))
{
    return new Response {
        Code = 400,
        Success = false,
        Message = 'Track ID cannot be null or empty.',
        Track = null
    };
}
```
Validate before calling service - fail fast, save resources.

**Level 2: HTTP Errors (404, 500, etc.)**
```csharp
catch (HttpRequestException ex)
{
    var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
    return new Response {
        Code = (int)statusCode,
        Success = false,
        Message = $'Failed: {ex.Message}',
        Track = null
    };
}
```
Preserve status codes from upstream API for proper categorization.

**Level 3: Unexpected Errors (500)**
```csharp
catch (Exception ex)
{
    return new Response {
        Code = 500,
        Success = false,
        Message = $'Unexpected error: {ex.Message}',
        Track = null
    };
}
```
Catch everything else - never let exceptions escape to GraphQL.

**Why no exceptions?**
- Predictable response structure (always has code/success/message/data)
- Type-safe for clients (TypeScript knows exact shape)
- User-friendly messages
- No GraphQL errors array (data is always present)

**Client benefits:**
```javascript
const result = await incrementViews({ id: 'c_0' });
if (result.success) {
  showToast(result.message);  // 'Successfully incremented views'
} else {
  if (result.code === 404) {
    navigate('/tracks');  // Track deleted, go to list
  } else {
    showError(result.message);  // Show specific error
  }
}
```

No try-catch needed, just check `success` field."

---

## Summary

In Stage 5, we implemented **GraphQL mutations** with professional error handling:

**What we created:**
- ✅ `Mutation` class with `IncrementTrackViews`
- ✅ Mutation response pattern (code/success/message/data)
- ✅ Three-level error handling (validation, HTTP, unexpected)
- ✅ 25 comprehensive tests

**Key learnings:**
- ✅ Mutations modify data and run sequentially
- ✅ Response types provide structured, type-safe error handling
- ✅ HTTP status codes in responses aid categorization
- ✅ Never throw exceptions to GraphQL layer
- ✅ Validate input before calling services
- ✅ Preserve error information from upstream APIs

**GraphQL best practices:**
- ✅ Return response types, not just data
- ✅ Include success boolean for quick checks
- ✅ Provide user-friendly messages
- ✅ Use HTTP-style status codes
- ✅ Handle all error scenarios gracefully

**Next up: Stage 6 - DataLoaders** where we'll solve the N+1 problem and dramatically improve query performance!

---

**Interview Readiness:** You can now explain mutations vs queries, the mutation response pattern, structured error handling, why we don't throw exceptions in GraphQL, and how to provide robust, user-friendly APIs! 🚀
