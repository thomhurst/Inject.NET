# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Inject.NET is a high-performance Dependency Injection library that leverages C# source generators to register types at compile time. By knowing types and scopes upfront, it optimizes creation and retrieval, removing unnecessary function resolutions and making injection significantly faster.

## Key Architecture

### Core Components

1. **Inject.NET** - Core library with attributes and interfaces
   - Attributes: `[ServiceProvider]`, `[Singleton]`, `[Scoped]`, `[Transient]`, `[WithTenant]`
   - Service lifetime management through `IServiceProvider`, `IServiceScope`, `IServiceRegistrar`

2. **Inject.NET.SourceGenerator** - Roslyn source generator
   - `DependenciesSourceGenerator.cs` - Main generator entry point
   - Writers folder contains code generation logic for different scopes
   - Generates optimized service provider implementations at compile time

3. **Test Projects**
   - Inject.NET.Tests - Integration tests using TUnit framework
   - Inject.NET.SourceGenerator.Tests - Source generator unit tests with Verify snapshots

## Development Commands

### Build
```bash
dotnet build
```

### Run Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Inject.NET.Tests/Inject.NET.Tests.csproj
dotnet test Inject.NET.SourceGenerator.Tests/Inject.NET.SourceGenerator.Tests.csproj

# Run with specific filter (TUnit framework)
dotnet test --filter "FullyQualifiedName~TestClassName"
```

### Benchmarks
```bash
cd Benchmarks
dotnet run -c Release
```

### Working with Source Generators

- **Important**: You must rebuild the Inject.NET.SourceGenerator project to see generated code changes in the IDE
- Generated code is output to `SourceGeneratedViewer` folder in test projects
- Use snapshot testing with Verify for source generator tests
- Debug source generators using the launch profile in `Inject.NET.SourceGenerator/Properties/launchSettings.json`

## Key Patterns

### Service Registration
Services are registered using attributes:
- `[ServiceProvider]` - Marks a class as a service provider
- `[Singleton]`, `[Scoped]`, `[Transient]` - Define service lifetimes
- `[WithTenant]` - Enables multi-tenancy support

### Source Generator Flow
1. Generator identifies classes marked with `[ServiceProvider]`
2. Collects all dependencies and their lifetimes
3. Generates optimized service provider code at compile time
4. Code includes proper disposal patterns and circular dependency detection

### Testing Approach
- Integration tests verify runtime behavior
- Source generator tests use Verify for snapshot testing of generated code
- Test files follow pattern: `TestName.verified.txt` for expected output

## Important Notes

- Solution uses .NET 9.0 and C# preview features
- TUnit is the test framework (not xUnit/NUnit)
- Source generators require rebuilding to see changes
- Verify snapshot tests auto-generate `.verified.txt` files