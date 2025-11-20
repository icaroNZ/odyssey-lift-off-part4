# Catstronauts GraphQL Server (C#)

A C# implementation of the Catstronauts GraphQL API using .NET 8 and Hot Chocolate.

## Project Structure

```
csharp-server/
├── Catstronauts.sln                  # Solution file (manages both projects)
├── Catstronauts.GraphQL/             # Web API project
│   ├── Program.cs                    # Application entry point
│   ├── GraphQL/
│   │   ├── Queries/                  # GraphQL query resolvers
│   │   ├── Mutations/                # GraphQL mutation resolvers
│   │   ├── Types/                    # Custom GraphQL types
│   │   └── DataLoaders/              # N+1 optimization
│   ├── Models/                       # Data transfer objects (DTOs)
│   └── Services/                     # Business logic & API clients
└── Catstronauts.Tests/               # Test project
    └── BasicTests.cs                 # Unit tests
```

## Prerequisites

- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Visual Studio Code (recommended) or Visual Studio 2022

## Setup Instructions

### 1. Install .NET 8 SDK

**macOS (using Homebrew):**
```bash
brew install dotnet@8
```

**macOS (manual):**
```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
export PATH="$PATH:$HOME/.dotnet"
```

**Verify installation:**
```bash
dotnet --version  # Should show 8.0.x
```

### 2. Restore NuGet Packages

```bash
cd csharp-server
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run the GraphQL Server

```bash
cd Catstronauts.GraphQL
dotnet run
```

The server will start at `http://localhost:5000`

### 5. Access GraphQL IDE

Open your browser and navigate to:
```
http://localhost:5000/graphql
```

You'll see **Banana Cake Pop**, Hot Chocolate's GraphQL IDE.

### 6. Try Your First Query

In the GraphQL IDE, run:

```graphql
query {
  hello
}
```

Expected response:
```json
{
  "data": {
    "hello": "Hello, GraphQL from C#!"
  }
}
```

## Running Tests

```bash
cd csharp-server
dotnet test
```

## Technology Stack

- **.NET 8**: Latest Long-Term Support (LTS) version
- **Hot Chocolate 13**: GraphQL server library
- **ASP.NET Core**: Web framework
- **xUnit**: Testing framework
- **NSubstitute**: Mocking library
- **FluentAssertions**: Readable test assertions

## Key Concepts Demonstrated

### 1. Solution Structure
- `.sln` file organizes multiple related projects
- Separation of concerns: web API vs. tests

### 2. Dependency Injection
- Services registered in `Program.cs`
- Hot Chocolate integrates with ASP.NET Core DI

### 3. Code-First GraphQL
- Schema defined using C# classes, not .graphql files
- Attributes like `[Query]` indicate operation types

### 4. Modern C# Features
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Common namespaces auto-imported
- **Top-level statements**: No need for `Main` method in Program.cs

## Next Steps

- **Stage 2**: Create data models (Track, Author, Module)
- **Stage 3**: Build service layer to call REST API
- **Stage 4**: Implement GraphQL queries
- **Stage 5**: Implement mutations
- **Stage 6**: Add DataLoaders for performance
- **Stage 7**: Comprehensive testing
- **Stage 8**: Documentation and deployment prep

## Troubleshooting

### NuGet Package Restore Fails
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Retry restore
dotnet restore
```

### Port Already in Use
```bash
# Change port in launchSettings.json or use:
dotnet run --urls "http://localhost:5001"
```

### Hot Reload Not Working
```bash
# Run with hot reload enabled
dotnet watch run
```

## Learning Resources

- [Hot Chocolate Documentation](https://chillicream.com/docs/hotchocolate/v13)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [xUnit Documentation](https://xunit.net/)

---

**Author**: Migration from Node.js/Apollo Server to C#/Hot Chocolate
**Date**: 2025-11-19
