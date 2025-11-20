# Stage 1: Foundation & Project Setup

## Overview

**What We're Building:** The foundational structure for a C# GraphQL server using .NET 8 and Hot Chocolate.

**Goal:** Create a working GraphQL server with a simple "Hello World" endpoint, properly configured project structure, and basic testing infrastructure.

**Duration:** 1-2 hours

**Status:** ✅ Completed

---

## Learning Objectives

By the end of this stage, you will understand:

- ✅ How .NET solution structure works (.sln manages multiple projects)
- ✅ The difference between web projects and test projects
- ✅ How NuGet packages work (similar to npm packages)
- ✅ How to configure C# projects (.csproj files)
- ✅ Hot Chocolate basics (creating a simple GraphQL endpoint)
- ✅ Basic testing setup with xUnit
- ✅ ASP.NET Core minimal API pattern
- ✅ Dependency injection fundamentals

---

## Prerequisites

- .NET 8 SDK installed
- Visual Studio Code with C# extension
- Basic understanding of GraphQL concepts
- Familiarity with command-line operations

---

## Key Concepts

### 1. .NET Solution Structure

#### What is a Solution?

A **solution** (.sln file) is a container that organizes and manages multiple related projects.

```
Catstronauts.sln  (Solution - the container)
├── Catstronauts.GraphQL  (Project 1 - Web API)
└── Catstronauts.Tests     (Project 2 - Tests)
```

#### Why Do We Need Solutions?

1. **Organization**: Keeps related projects together
2. **Build Management**: `dotnet build` at solution level builds all projects
3. **Dependencies**: Projects can reference each other (tests → web API)
4. **IDE Support**: VS Code understands the entire codebase structure

#### Real-World Analogy

Think of a solution like a **filing cabinet**:
- The cabinet itself is the **solution file** (.sln)
- Each drawer is a **project** (.csproj)
- Files within drawers are **source code files**

The cabinet keeps everything organized and lets you access related materials together.

#### Code Example

Creating a solution:
```bash
dotnet new sln -n Catstronauts
```

This generates:
```
Catstronauts.sln  # Text file listing all projects
```

#### Interview Answer

> "A .NET solution is an organizational container for multiple related projects. In our GraphQL application, we use a solution to manage both our web API project and our test project. This separation allows us to keep concerns separated—production code in one project, test code in another—while maintaining the ability to build and deploy them together. It's conceptually similar to a monorepo workspace in JavaScript, but with stronger tooling integration."

---

### 2. Project Types: Web vs. Test

#### Web Project (Catstronauts.GraphQL)

**Purpose:** Hosts the GraphQL API

**SDK:** `Microsoft.NET.Sdk.Web`
- Includes ASP.NET Core framework
- HTTP server capabilities
- Middleware pipeline
- Dependency injection container

**Output:** Runnable web application (executable)

**Key Properties:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

#### Test Project (Catstronauts.Tests)

**Purpose:** Contains unit and integration tests

**SDK:** `Microsoft.NET.Sdk`
- Standard .NET library SDK
- No web server needed
- References the web project for testing

**Output:** Test runner executable (not deployed)

**Key Properties:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>      ← Cannot be packaged as NuGet
    <IsTestProject>true</IsTestProject>  ← IDE recognizes as test project
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Catstronauts.GraphQL\..." />
  </ItemGroup>
</Project>
```

#### Why Separate Projects?

1. **Deployment**: Test code never ships to production
2. **Dependencies**: Tests need testing libraries (xUnit, NSubstitute)
3. **Organization**: Clear boundary between production and test code
4. **Build Speed**: Can build web project without test dependencies

#### Interview Answer

> "We separate web and test projects to maintain clear boundaries. The web project uses the `Microsoft.NET.Sdk.Web` SDK, which includes ASP.NET Core for hosting HTTP endpoints. The test project uses the standard SDK and references the web project. This ensures test code and test dependencies never make it into production deployments. It also makes the codebase easier to navigate—developers immediately know whether they're looking at production code or tests."

---

### 3. .csproj Files (Project Configuration)

#### What is a .csproj File?

An **XML file** that defines:
- Target framework (which .NET version)
- Package dependencies (NuGet packages)
- Project references (links to other projects)
- Build settings (compiler options)

**Analogy:** It's like `package.json` + `tsconfig.json` combined.

#### Key Settings Explained

##### 1. TargetFramework
```xml
<TargetFramework>net8.0</TargetFramework>
```

**What it does:** Specifies which .NET version to compile against

**Why net8.0?**
- Long-Term Support (LTS) version
- 3 years of support (until November 2026)
- Production-ready and stable
- Latest features and performance improvements

**Interview Tip:** "We use .NET 8 because it's the latest LTS version, giving us 3 years of support and the best performance characteristics."

##### 2. Nullable Reference Types
```xml
<Nullable>enable</Nullable>
```

**What it does:** Enables C# 8+ nullable reference types

**Impact:**
```csharp
// Without nullable enabled
string name = null;  // Compiles fine, crashes at runtime

// With nullable enabled
string name = null;   // ⚠️ Compiler warning!
string? name = null;  // ✅ OK, explicitly nullable
```

**Benefits:**
- Prevents `NullReferenceException` (the "billion-dollar mistake")
- Self-documenting code (`Track?` means "might be null")
- Better IDE IntelliSense

**Interview Answer:**
> "We enable nullable reference types to catch null-related bugs at compile time instead of runtime. This is a game-changer for production reliability. When you see `Track?`, you know it might be null and need to handle that case. When you see `Track`, the compiler guarantees it's not null. This prevents the dreaded NullReferenceException that can crash production systems."

##### 3. Implicit Usings
```xml
<ImplicitUsings>enable</ImplicitUsings>
```

**What it does:** Automatically imports common namespaces

**Auto-imported for web projects:**
```csharp
// These are automatically available:
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
```

**Benefit:** Less boilerplate, cleaner code

**Trade-off:** Less explicit about dependencies (minor issue)

##### 4. LangVersion
```xml
<LangVersion>latest</LangVersion>
```

**What it does:** Uses the latest C# language features

**For .NET 8:** Enables C# 12 features
- Primary constructors
- Collection expressions
- Improved pattern matching
- Ref readonly parameters

**Why "latest"?**
- Access to newest language features
- Better syntax and expressiveness
- Performance optimizations

---

### 4. NuGet Packages (Dependency Management)

#### What is NuGet?

.NET's package manager, equivalent to npm for Node.js.

**Package Registry:** nuget.org (public package repository)

**Commands:**
```bash
dotnet add package HotChocolate.AspNetCore    # Add package
dotnet restore                                # Download packages
dotnet list package                           # List installed packages
```

#### Packages We Installed

##### HotChocolate.AspNetCore (13.9.14)

**Purpose:** GraphQL server library for .NET

**Why Hot Chocolate?**
- ✅ Best performance among .NET GraphQL libraries
- ✅ Active development and community
- ✅ Excellent documentation
- ✅ Built-in DataLoader support (N+1 prevention)
- ✅ Code-first approach (define schema with C# classes)
- ✅ Integrates seamlessly with ASP.NET Core

**Alternatives Considered:**
- `GraphQL.NET`: Older, less performant
- `GraphQL-dotnet`: Schema-first only

**Installation:**
```xml
<PackageReference Include="HotChocolate.AspNetCore" Version="13.9.14" />
```

##### xUnit (2.5.3)

**Purpose:** Unit testing framework

**Why xUnit?**
- ✅ Modern design (no test class inheritance required)
- ✅ Better parallel test execution
- ✅ Industry standard for .NET
- ✅ Used by Microsoft for .NET Core itself
- ✅ Extensible with attributes

**Alternatives:**
- `NUnit`: Older, Java-inspired
- `MSTest`: Microsoft's original framework

**Key Features:**
```csharp
[Fact]  // Simple test
public void Test1() { }

[Theory]  // Parameterized test
[InlineData(1, 2, 3)]
[InlineData(2, 3, 5)]
public void Add_Test(int a, int b, int expected) { }
```

##### FluentAssertions (6.12.0)

**Purpose:** Readable assertion library

**Why FluentAssertions?**
- ✅ Natural language assertions
- ✅ Better error messages
- ✅ IntelliSense support
- ✅ Extensive assertion methods

**Comparison:**
```csharp
// Standard assertions
Assert.Equal(2, result);
Assert.True(list.Count > 0);

// FluentAssertions
result.Should().Be(2);
list.Should().NotBeEmpty();
```

The fluent syntax reads like English and provides much better error messages when tests fail.

##### NSubstitute (5.1.0)

**Purpose:** Mocking library for unit tests

**Why NSubstitute?**
- ✅ Simple, intuitive API
- ✅ Less ceremony than Moq
- ✅ Clear syntax for setup and verification

**Example:**
```csharp
// Create mock
var mockService = Substitute.For<ITrackService>();

// Setup behavior
mockService.GetTrackAsync("t_01")
    .Returns(new Track { Id = "t_01", Title = "Test" });

// Use in test
var result = await mockService.GetTrackAsync("t_01");
result.Title.Should().Be("Test");

// Verify call
mockService.Received(1).GetTrackAsync("t_01");
```

#### Interview Answer

> "We chose Hot Chocolate for GraphQL because it's the most performant and feature-rich option for .NET, with excellent DataLoader support for preventing N+1 queries. For testing, we use xUnit as the modern standard—it's what Microsoft uses internally. FluentAssertions makes our tests readable with natural language syntax, and NSubstitute provides simple mocking with minimal setup code. This combination gives us a solid foundation for test-driven development."

---

### 5. Program.cs (Application Entry Point)

#### Modern C# Program Structure

**Before C# 10:** Required explicit Main method
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.Run();
    }
}
```

**C# 10+ (Top-Level Statements):** Simplified
```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();
```

The compiler automatically generates the Main method.

#### Our GraphQL Server Configuration

```csharp
using Catstronauts.GraphQL.GraphQL.Queries;

// Phase 1: Configuration (build the DI container)
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()           // Register GraphQL services
    .AddQueryType<Query>();       // Tell HC where queries are

// Phase 2: Build the application
var app = builder.Build();

// Phase 3: Configure middleware pipeline
app.MapGraphQL();                 // Create /graphql endpoint

// Phase 4: Start the server
app.Run();
```

#### Two-Phase Setup: Configuration vs. Middleware

**Configuration Phase (`builder.Services`)**
- Registers services in the DI container
- Configures options
- Prepares dependencies

**Middleware Phase (`app.Map...`)**
- Defines HTTP endpoints
- Sets up request processing pipeline
- Configures routing

**Analogy:**
- **Configuration** = Preparing ingredients before cooking
- **Middleware** = Actual cooking steps

#### Key Methods Explained

##### AddGraphQLServer()
```csharp
builder.Services.AddGraphQLServer()
```

**What it does:**
- Registers Hot Chocolate services with DI container
- Sets up GraphQL execution engine
- Configures schema builder
- Registers default services (caching, validation, etc.)

##### AddQueryType<Query>()
```csharp
.AddQueryType<Query>()
```

**What it does:**
- Tells Hot Chocolate which class contains root queries
- Hot Chocolate scans the class for public methods
- Each method becomes a GraphQL field
- Automatically handles type conversion

##### MapGraphQL()
```csharp
app.MapGraphQL();
```

**What it does:**
- Creates HTTP endpoint at `/graphql`
- Handles POST requests (GraphQL queries)
- Serves Banana Cake Pop IDE on GET requests
- Processes GraphQL over HTTP protocol

**Default endpoint:** `http://localhost:5000/graphql`

#### Interview Answer

> "ASP.NET Core uses a two-phase setup pattern. First, we configure services in the dependency injection container using `builder.Services`. This is where we register Hot Chocolate with `AddGraphQLServer()`. Second, we configure the middleware pipeline with `app.MapGraphQL()`, which creates the actual HTTP endpoint. This separation allows for flexible, testable architecture. Hot Chocolate integrates seamlessly because it's designed specifically for ASP.NET Core's DI system."

---

### 6. Query Class (GraphQL Resolvers)

#### Code-First Schema Definition

```csharp
namespace Catstronauts.GraphQL.GraphQL.Queries;

public class Query
{
    public string GetHello() => "Hello, GraphQL from C#!";
}
```

#### How Hot Chocolate Transforms This

**C# Code:**
```csharp
public class Query
{
    public string GetHello() => "Hello, GraphQL from C#!";
}
```

**Generated GraphQL Schema:**
```graphql
type Query {
  hello: String!
}
```

#### Transformation Rules

1. **Class Name → Type Name**
   - `Query` class → `Query` type
   - Convention-based (can override with attributes)

2. **Method Name → Field Name**
   - `GetHello()` → `hello`
   - `GetTracksForHome()` → `tracksForHome`
   - Strips "Get" prefix
   - Converts to camelCase

3. **Return Type → GraphQL Type**
   - `string` → `String!` (non-null)
   - `string?` → `String` (nullable)
   - `int` → `Int!`
   - `List<Track>` → `[Track!]!`

4. **Parameters → Arguments**
   - `GetTrack(string id)` → `track(id: String!)`

#### Code-First vs. Schema-First

**Schema-First:**
```graphql
# Define schema first
type Query {
  hello: String!
}
```
Then generate C# classes from schema.

**Code-First (our approach):**
```csharp
// Define C# classes first
public class Query
{
    public string GetHello() => "Hello!";
}
```
Then Hot Chocolate generates schema from code.

**Why Code-First?**

✅ **Compile-time safety**
- Type errors caught by compiler
- No schema-code mismatches

✅ **Refactoring support**
- Rename method → schema updates automatically
- IDE refactoring tools work

✅ **No code generation**
- Simpler build process
- No intermediate files to manage

✅ **IntelliSense**
- Full IDE support for C# code
- Type hints while coding

❌ **Less explicit schema**
- Schema is derived from code
- Requires tools to view full schema

#### Interview Answer

> "We use Hot Chocolate's code-first approach where we define our schema using C# classes. The framework scans our classes at startup and automatically generates the GraphQL schema. Method names like `GetHello` become GraphQL fields like `hello`, following GraphQL's camelCase convention. This gives us compile-time safety—type errors are caught during build, not at runtime. It also means refactoring is safe: rename a method and the schema updates automatically. The trade-off is the schema is implicit, but Hot Chocolate provides tools to export it for documentation."

---

### 7. Test Structure (AAA Pattern)

#### The AAA Pattern

**AAA = Arrange, Act, Assert**

```csharp
[Fact]
public void SanityCheck_OnePlusOne_EqualsTwo()
{
    // Arrange: Set up test data and preconditions
    var a = 1;
    var b = 1;

    // Act: Execute the code under test
    var result = a + b;

    // Assert: Verify the expected outcome
    result.Should().Be(2);
}
```

#### Why AAA Pattern?

1. **Readability**: Clear structure, easy to understand
2. **Consistency**: All tests follow same pattern
3. **Debugging**: Quickly identify which phase failed
4. **Code Review**: Easy to verify test correctness

#### Each Phase Explained

##### Arrange
**Purpose:** Set up test preconditions

**What to include:**
- Test data creation
- Mock configuration
- Initial state setup

**Example:**
```csharp
// Arrange
var mockService = Substitute.For<ITrackService>();
mockService.GetTrackAsync("t_01")
    .Returns(new Track { Id = "t_01", Title = "Test Track" });
var query = new Query();
```

##### Act
**Purpose:** Execute the code being tested

**Best practice:** Usually one line

**Example:**
```csharp
// Act
var result = await query.GetTrack("t_01", mockService);
```

##### Assert
**Purpose:** Verify the outcome

**What to check:**
- Return values
- State changes
- Method calls (verification)

**Example:**
```csharp
// Assert
result.Should().NotBeNull();
result.Id.Should().Be("t_01");
result.Title.Should().Be("Test Track");
```

#### Test Naming Convention

**Format:** `MethodName_Scenario_ExpectedBehavior`

**Examples:**
- ✅ `GetTrack_WithValidId_ReturnsTrack`
- ✅ `GetTrack_WithNullId_ThrowsArgumentException`
- ✅ `GetTrack_WhenApiReturns404_ReturnsNull`
- ❌ `Test1`
- ❌ `TestGetTrack`

**Why this matters:**
- Test name explains what's being tested
- When test fails, you know exactly what broke
- Serves as living documentation

#### Interview Answer

> "We follow the AAA pattern for all tests—it's the industry standard. Arrange sets up preconditions and test data. Act executes the code under test, typically in one line. Assert verifies the outcome using FluentAssertions for readability. Our naming convention `Method_Scenario_Expected` makes test intentions crystal clear. When a test fails in CI/CD, the name immediately tells us what broke without needing to read the code. This is crucial for rapid debugging in production incidents."

---

## Implementation Steps

### Step 1: Install .NET 8 SDK

**macOS:**
```bash
# Using official installer script
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

# Add to PATH
export PATH="$PATH:$HOME/.dotnet"
```

**Verify installation:**
```bash
dotnet --version
# Should output: 8.0.xxx
```

### Step 2: Create Solution and Projects

```bash
# Create directory
mkdir csharp-server
cd csharp-server

# Create solution
dotnet new sln -n Catstronauts

# Create web API project
dotnet new web -n Catstronauts.GraphQL -o Catstronauts.GraphQL

# Create test project
dotnet new xunit -n Catstronauts.Tests -o Catstronauts.Tests

# Add projects to solution
dotnet sln add Catstronauts.GraphQL/Catstronauts.GraphQL.csproj
dotnet sln add Catstronauts.Tests/Catstronauts.Tests.csproj

# Add project reference (tests → web)
cd Catstronauts.Tests
dotnet add reference ../Catstronauts.GraphQL/Catstronauts.GraphQL.csproj
cd ..
```

**Result:**
```
csharp-server/
├── Catstronauts.sln
├── Catstronauts.GraphQL/
│   └── Catstronauts.GraphQL.csproj
└── Catstronauts.Tests/
    └── Catstronauts.Tests.csproj
```

### Step 3: Configure Project Files

**Edit `Catstronauts.GraphQL/Catstronauts.GraphQL.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="HotChocolate.AspNetCore" Version="13.9.14" />
  </ItemGroup>

</Project>
```

**Edit `Catstronauts.Tests/Catstronauts.Tests.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="NSubstitute" Version="5.1.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Catstronauts.GraphQL\Catstronauts.GraphQL.csproj" />
  </ItemGroup>

</Project>
```

### Step 4: Create Folder Structure

```bash
cd Catstronauts.GraphQL
mkdir -p Models Services GraphQL/Queries GraphQL/Mutations GraphQL/Types GraphQL/DataLoaders
cd ..
```

**Result:**
```
Catstronauts.GraphQL/
├── Models/           # Data objects
├── Services/         # Business logic
└── GraphQL/
    ├── Queries/      # Query resolvers
    ├── Mutations/    # Mutation resolvers
    ├── Types/        # Custom types
    └── DataLoaders/  # N+1 prevention
```

### Step 5: Create Query Class

**File: `Catstronauts.GraphQL/GraphQL/Queries/Query.cs`**

```csharp
namespace Catstronauts.GraphQL.GraphQL.Queries;

/// <summary>
/// Root query type for the GraphQL API.
/// Contains all top-level query operations.
/// </summary>
public class Query
{
    /// <summary>
    /// Simple "Hello World" query for testing GraphQL setup.
    /// </summary>
    /// <returns>A greeting message</returns>
    public string GetHello() => "Hello, GraphQL from C#!";
}
```

**Why this works:**
- Method name `GetHello` → GraphQL field `hello`
- Return type `string` → GraphQL type `String!`
- XML comments → GraphQL description (with proper configuration)

### Step 6: Configure Program.cs

**File: `Catstronauts.GraphQL/Program.cs`**

```csharp
using Catstronauts.GraphQL.GraphQL.Queries;

var builder = WebApplication.CreateBuilder(args);

// Register GraphQL server with Hot Chocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

// Map the GraphQL endpoint to /graphql
app.MapGraphQL();

app.Run();
```

**Line-by-line breakdown:**

1. **`using Catstronauts.GraphQL.GraphQL.Queries;`**
   - Import namespace for Query class
   - Allows referencing `Query` without full namespace

2. **`var builder = WebApplication.CreateBuilder(args);`**
   - Creates web application builder
   - Configures defaults (logging, configuration, DI)
   - `args` = command-line arguments

3. **`builder.Services.AddGraphQLServer()`**
   - Registers Hot Chocolate with DI container
   - Returns `IRequestExecutorBuilder` for chaining

4. **`.AddQueryType<Query>()`**
   - Tells Hot Chocolate where queries are defined
   - Scans `Query` class for public methods
   - Generates schema from method signatures

5. **`var app = builder.Build();`**
   - Builds the web application
   - Finalizes DI container
   - Creates middleware pipeline

6. **`app.MapGraphQL();`**
   - Creates `/graphql` endpoint
   - Handles GraphQL queries (POST requests)
   - Serves Banana Cake Pop IDE (GET requests)

7. **`app.Run();`**
   - Starts the web server
   - Listens for HTTP requests
   - Blocks until application shutdown

### Step 7: Create Basic Tests

**File: `Catstronauts.Tests/BasicTests.cs`**

```csharp
using FluentAssertions;
using Xunit;

namespace Catstronauts.Tests;

/// <summary>
/// Basic sanity tests to verify test setup is working.
/// </summary>
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

    [Fact]
    public void String_Concatenation_Works()
    {
        // Arrange
        var firstName = "GraphQL";
        var lastName = "Rocks";

        // Act
        var fullName = $"{firstName} {lastName}";

        // Assert
        fullName.Should().Be("GraphQL Rocks");
    }
}
```

### Step 8: Build and Run

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the GraphQL server
cd Catstronauts.GraphQL
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Step 9: Test the GraphQL Endpoint

1. Open browser: `http://localhost:5000/graphql`
2. You'll see **Banana Cake Pop** (Hot Chocolate's GraphQL IDE)
3. Run query:

```graphql
query {
  hello
}
```

4. Expected response:

```json
{
  "data": {
    "hello": "Hello, GraphQL from C#!"
  }
}
```

---

## Design Decisions & Trade-offs

### Decision 1: Code-First vs. Schema-First

**Options:**
1. **Schema-First**: Write .graphql schema, generate C# classes
2. **Code-First**: Write C# classes, generate GraphQL schema ← **We chose this**

**Comparison:**

| Aspect | Code-First | Schema-First |
|--------|------------|--------------|
| Type Safety | ✅ Compile-time | ⚠️ Runtime (if out of sync) |
| Refactoring | ✅ IDE support | ⚠️ Manual sync needed |
| Explicitness | ❌ Schema derived | ✅ Schema is source of truth |
| Build Process | ✅ Simple | ⚠️ Code generation step |
| Learning Curve | ⚠️ Framework-specific | ✅ GraphQL-standard |

**Why Code-First?**
- Leverages C# type system
- Safer refactoring
- No code generation step
- Better IDE support

**Trade-off:**
- Schema is less explicit
- Requires Hot Chocolate knowledge

**Interview Answer:**
> "We chose code-first because it leverages C#'s strong type system. When we refactor, the schema automatically updates—no manual synchronization. We get compile-time errors instead of runtime schema mismatches. The trade-off is the schema is implicit, but Hot Chocolate provides a schema export command for documentation. For teams already invested in .NET, code-first is more maintainable long-term."

### Decision 2: Minimal API vs. Controller-Based

**Options:**
1. **Minimal API** (top-level statements) ← **We chose this**
2. **Controller-Based** (traditional MVC pattern)

**Minimal API:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQLServer().AddQueryType<Query>();
var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**Controller-Based:**
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGraphQLServer().AddQueryType<Query>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapGraphQL());
    }
}
```

**Why Minimal API?**
- ✅ Less boilerplate (5 lines vs 20+)
- ✅ Faster startup time
- ✅ Modern .NET pattern (.NET 6+)
- ✅ Sufficient for single-endpoint APIs
- ✅ Easier for beginners

**When to use Controllers?**
- Many REST endpoints
- Complex middleware configuration
- Attribute-based routing needed

**Interview Answer:**
> "For a GraphQL API with a single endpoint, minimal API is perfect. It reduces boilerplate significantly compared to the controller-based approach. We still get full dependency injection, middleware, and configuration—just with less ceremony. If we were building a REST API with dozens of endpoints, controllers might provide better organization. But for GraphQL, minimal API is the modern, cleaner approach."

### Decision 3: xUnit vs. Other Testing Frameworks

**Options:**
1. **xUnit** ← **We chose this**
2. **NUnit**
3. **MSTest**

**Comparison:**

| Feature | xUnit | NUnit | MSTest |
|---------|-------|-------|--------|
| Parallel Execution | ✅ Built-in | ⚠️ Manual config | ❌ Limited |
| Setup/Teardown | ✅ Constructor | `[SetUp]` | `[TestInitialize]` |
| Test Isolation | ✅ New instance per test | ⚠️ Shared instance | ⚠️ Shared instance |
| Modern Features | ✅ | ⚠️ | ❌ |
| Microsoft Usage | ✅ .NET Core | ❌ | ✅ Old projects |

**Why xUnit?**
- Modern design (no inheritance needed)
- Better parallel execution
- Each test gets fresh instance (true isolation)
- Industry standard for new .NET projects
- Microsoft uses it for .NET Core

**Interview Answer:**
> "We chose xUnit because it's the modern standard for .NET testing. Unlike older frameworks, it doesn't require test classes to inherit from a base class, and it provides true test isolation—each test gets a new class instance. It also has superior parallel execution, which speeds up test runs. Microsoft uses xUnit for testing .NET Core itself, which is a strong endorsement."

---

## Troubleshooting

### Issue: "dotnet: command not found"

**Cause:** .NET SDK not in PATH

**Solution (macOS):**
```bash
# Add to ~/.zshrc or ~/.bash_profile
export PATH="$PATH:$HOME/.dotnet"

# Reload shell
source ~/.zshrc
```

### Issue: "Unable to load the service index for source https://api.nuget.org"

**Cause:** Network connectivity or firewall blocking NuGet

**Solutions:**
```bash
# 1. Clear NuGet cache
dotnet nuget locals all --clear

# 2. Retry restore
dotnet restore

# 3. Check network/VPN
# Try disabling VPN temporarily

# 4. Use offline restore (if packages cached)
dotnet restore --no-http-cache
```

### Issue: Build fails with "The type or namespace name 'HotChocolate' could not be found"

**Cause:** NuGet packages not restored

**Solution:**
```bash
# Navigate to project directory
cd Catstronauts.GraphQL

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

### Issue: Port 5000 already in use

**Symptoms:**
```
Unable to bind to http://localhost:5000 on the IPv4 loopback interface
```

**Solutions:**
```bash
# Option 1: Find and kill process using port 5000
lsof -i :5000
kill -9 <PID>

# Option 2: Use different port
dotnet run --urls "http://localhost:5001"

# Option 3: Edit launchSettings.json
# Change applicationUrl to different port
```

### Issue: Tests fail with "No test is available in..."

**Cause:** Test project not properly configured

**Solution:**
```bash
# Ensure test project has correct SDK
# In .csproj:
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
</Project>

# Rebuild
dotnet build

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Issue: GraphQL IDE not loading at /graphql

**Cause:** Endpoint not mapped or Hot Chocolate not configured

**Checklist:**
1. Verify `app.MapGraphQL();` in Program.cs
2. Ensure Hot Chocolate package installed
3. Check browser console for errors
4. Try accessing via POST (Postman/curl):

```bash
curl -X POST http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ hello }"}'
```

---

## Interview Preparation

### Common Questions

#### Q1: "Why use .NET for a GraphQL server instead of Node.js?"

**Answer:**

> "Both are excellent choices with different strengths. We chose .NET for several reasons:
>
> **Performance:** .NET is compiled to native code via JIT, giving significantly better CPU and memory performance than interpreted JavaScript, especially under high load.
>
> **Type Safety:** C# is statically typed with compile-time checking. We catch type errors during build, not in production. With nullable reference types enabled, we even prevent NullReferenceExceptions at compile time.
>
> **Ecosystem:** .NET has mature libraries for enterprise scenarios—authentication, caching, database access, and monitoring are all battle-tested at massive scale.
>
> **Tooling:** Visual Studio and Rider provide exceptional debugging, profiling, and refactoring tools that are unmatched for GraphQL development.
>
> **Scalability:** .NET's async runtime and thread pool are optimized for high-concurrency scenarios. We can handle thousands of concurrent GraphQL queries efficiently.
>
> That said, Node.js excels at rapid prototyping and has a massive npm ecosystem. For teams already invested in JavaScript, Apollo Server is fantastic. We chose .NET because we value compile-time safety and performance for production systems at scale."

#### Q2: "Explain dependency injection in ASP.NET Core"

**Answer:**

> "Dependency injection is a design pattern where objects receive their dependencies from external sources rather than creating them. ASP.NET Core has a built-in DI container that manages object lifetimes.
>
> Here's how it works in our GraphQL server:
>
> **Registration (in Program.cs):**
> ```csharp
> builder.Services.AddScoped<ITrackService, TrackService>();
> ```
>
> This tells the DI container: 'When someone needs an ITrackService, give them a TrackService instance.'
>
> **Injection (in resolver):**
> ```csharp
> public async Task<List<Track>> GetTracksForHome(
>     [Service] ITrackService trackService)
> {
>     return await trackService.GetTracksForHomeAsync();
> }
> ```
>
> The `[Service]` attribute tells Hot Chocolate to resolve `ITrackService` from the DI container.
>
> **Benefits:**
>
> 1. **Testability:** In tests, we inject mocks instead of real services
> 2. **Loose Coupling:** Code depends on interfaces, not concrete classes
> 3. **Lifetime Management:** Container handles object creation and disposal
> 4. **Configuration:** Can swap implementations without changing code
>
> **Lifetimes:**
>
> - **Transient:** New instance every time (AddTransient)
> - **Scoped:** One instance per HTTP request (AddScoped) ← We use this for services
> - **Singleton:** One instance for application lifetime (AddSingleton)
>
> For GraphQL resolvers, we use Scoped because we want fresh service instances per request but shared within that request."

#### Q3: "What are nullable reference types and why enable them?"

**Answer:**

> "Nullable reference types are a C# 8+ feature that makes the type system distinguish between nullable and non-nullable references, preventing the 'billion-dollar mistake' of null reference exceptions.
>
> **Without nullable types enabled:**
> ```csharp
> string name = null;  // Compiles fine, crashes at runtime with NullReferenceException
> Console.WriteLine(name.Length);  // 💥 Boom!
> ```
>
> **With `<Nullable>enable</Nullable>`:**
> ```csharp
> string name = null;   // ⚠️ Warning CS8600: Converting null literal to non-nullable type
> string? name = null;  // ✅ OK, explicitly nullable
>
> if (name != null) {
>     Console.WriteLine(name.Length);  // Compiler knows this is safe
> }
> ```
>
> **Benefits:**
>
> 1. **Prevents NullReferenceException:** Caught at compile time, not production
> 2. **Self-Documenting:** `Track?` means 'might be null', `Track` means 'never null'
> 3. **Better IDE Support:** IntelliSense knows when null checks are needed
> 4. **API Contracts:** Return types explicitly state nullability expectations
>
> **In our GraphQL API:**
> ```csharp
> public async Task<Track?> GetTrackAsync(string id)
> //                     ↑ Might return null if track not found
> ```
>
> This tells callers they must handle the null case. Without the `?`, the compiler would warn us if we try to return null.
>
> It's a paradigm shift that requires discipline, but it dramatically improves code quality and prevents an entire class of production bugs. Every new .NET project should enable it."

#### Q4: "How does Hot Chocolate generate the GraphQL schema from C# code?"

**Answer:**

> "Hot Chocolate uses reflection and convention-based mapping to generate the GraphQL schema at application startup. Here's the process:
>
> **Step 1: Class Discovery**
> When we call `.AddQueryType<Query>()`, Hot Chocolate identifies the root Query class.
>
> **Step 2: Method Scanning**
> It uses reflection to scan all public methods in the class:
> ```csharp
> public string GetHello() => "Hello!";
> ```
>
> **Step 3: Type Mapping**
> It maps C# types to GraphQL types:
> - `string` → `String!` (non-null)
> - `string?` → `String` (nullable)
> - `int` → `Int!`
> - `List<Track>` → `[Track!]!`
>
> **Step 4: Naming Conversion**
> Method names are converted to GraphQL field names:
> - `GetHello()` → `hello` (strips 'Get', converts to camelCase)
> - `GetTracksForHome()` → `tracksForHome`
>
> **Step 5: Schema Generation**
> It generates the GraphQL schema in memory:
> ```graphql
> type Query {
>   hello: String!
> }
> ```
>
> **Customization:**
> We can override defaults with attributes:
> ```csharp
> [GraphQLName("greeting")]
> public string GetHello() => "Hello!";
> ```
>
> **Schema Export:**
> We can export the generated schema for documentation:
> ```bash
> dotnet run -- schema export --output schema.graphql
> ```
>
> The beauty of this approach is that our C# code is the single source of truth. Refactor a method name, and the schema updates automatically. No manual synchronization needed."

#### Q5: "Explain the AAA testing pattern"

**Answer:**

> "AAA stands for Arrange, Act, Assert—it's the industry-standard pattern for structuring unit tests. It provides consistency and readability across all tests.
>
> **Structure:**
> ```csharp
> [Fact]
> public async Task GetTrack_WithValidId_ReturnsTrack()
> {
>     // Arrange: Set up test preconditions
>     var mockService = Substitute.For<ITrackService>();
>     mockService.GetTrackAsync("t_01")
>         .Returns(new Track { Id = "t_01", Title = "Test Track" });
>     var query = new Query();
>
>     // Act: Execute the code under test
>     var result = await query.GetTrack("t_01", mockService);
>
>     // Assert: Verify the expected outcome
>     result.Should().NotBeNull();
>     result.Id.Should().Be("t_01");
>     result.Title.Should().Be("Test Track");
> }
> ```
>
> **Each Phase Explained:**
>
> **Arrange:**
> - Create test data
> - Configure mocks and dependencies
> - Set up initial state
> - This phase can be multiple lines
>
> **Act:**
> - Execute the method being tested
> - Usually just one line
> - Should be the only line that changes system state
>
> **Assert:**
> - Verify the result matches expectations
> - Check multiple conditions if needed
> - We use FluentAssertions for readable syntax
>
> **Benefits:**
>
> 1. **Readability:** Anyone can understand test intent immediately
> 2. **Debugging:** When test fails, you know which phase broke
> 3. **Consistency:** All tests follow same structure
> 4. **Code Reviews:** Easy to verify test correctness
>
> **Naming Convention:**
> We pair AAA with descriptive naming: `MethodName_Scenario_ExpectedBehavior`
>
> Example: `GetTrack_WithNullId_ThrowsArgumentException`
>
> When this test fails in CI/CD, the name tells us exactly what broke without reading code. This is critical for rapid incident response in production systems."

---

## Summary

### What We Accomplished

✅ **Project Structure**
- Created .NET solution with web and test projects
- Configured modern C# features (nullable types, implicit usings)
- Organized folder structure for scalability

✅ **Dependencies**
- Installed Hot Chocolate for GraphQL
- Set up xUnit, FluentAssertions, and NSubstitute for testing
- Configured all packages in .csproj files

✅ **GraphQL Server**
- Created working "Hello World" endpoint
- Configured Hot Chocolate with code-first approach
- Set up Program.cs with dependency injection

✅ **Testing Infrastructure**
- Established AAA pattern for tests
- Created basic sanity tests
- Configured test project with proper references

✅ **Documentation**
- Created comprehensive README
- Added .gitignore for .NET projects
- Documented all setup steps

### Key Takeaways

1. **.NET Solutions organize multiple projects** into a cohesive unit
2. **Code-first GraphQL** leverages C#'s type system for safety
3. **Dependency injection** is core to ASP.NET Core architecture
4. **Nullable reference types** prevent null-related bugs
5. **AAA pattern** provides consistent, readable test structure
6. **Hot Chocolate** generates GraphQL schema from C# classes

### Skills Gained

- ✅ Creating .NET solutions and projects
- ✅ Managing NuGet dependencies
- ✅ Configuring ASP.NET Core applications
- ✅ Setting up Hot Chocolate GraphQL
- ✅ Writing unit tests with xUnit
- ✅ Using FluentAssertions for readable tests
- ✅ Understanding dependency injection

### Interview-Ready Topics

- Why .NET for GraphQL servers
- Solution and project architecture
- Code-first vs. schema-first GraphQL
- Nullable reference types benefits
- Dependency injection pattern
- AAA testing pattern
- Hot Chocolate schema generation

---

## Connection to Next Stage

**Stage 2: Data Models & DTOs**

Now that we have a working GraphQL server, we need to define the **data structures** for our application:

- `Track`: Represents a learning course
- `Author`: Content creator information
- `Module`: Individual lesson within a track
- `IncrementTrackViewsResponse`: Mutation response object

We'll learn:
- C# classes and properties
- POCOs (Plain Old C# Objects)
- Property types and nullability
- XML documentation for models
- Model validation concepts

**Why this order?**
Foundation → Models → Services → GraphQL

We build from the bottom up:
1. ✅ **Foundation** (this stage): Infrastructure
2. **Models** (next stage): Data structures
3. **Services** (later): Business logic using models
4. **GraphQL** (later): Expose services via GraphQL

Each stage builds on the previous, creating a solid, testable architecture.

---

**Stage Status:** ✅ Complete
**Next Stage:** [Stage 2: Data Models & DTOs](./Stage-02-Models.md)
**Return to:** [Documentation Index](./INDEX.md)

---

**Last Updated:** 2025-11-20
**Author:** Claude (Senior C# & GraphQL Mentor)
