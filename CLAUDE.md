# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Inject.NET is a high-performance Dependency Injection library that leverages C# source generators to register types at compile time. By knowing types and scopes upfront, it optimizes creation and retrieval, removing unnecessary function resolutions and making injection significantly faster.

## Project Structure

- **Inject.NET** - Core library (targets .NET 8.0 & 9.0)
  - `Attributes/` - All DI attributes (`[ServiceProvider]`, `[Singleton]`, `[Scoped]`, `[Transient]`, `[WithTenant]`, decorators, etc.)
  - `Types.cs`, `ThrowHelpers.cs`, `Pools.cs` - Runtime utilities

- **Inject.NET.SourceGenerator** - Roslyn incremental source generator
  - `DependenciesSourceGenerator.cs` - Main entry point, implements `IIncrementalGenerator`
  - `Writers/` - Code generation for different provider types (ServiceProvider, Scope, Tenant, etc.)
  - `Models/` - Internal models representing services and their dependencies
  - Helpers: `TypeCollector`, `DependencyDictionary`, `DecoratorDictionary`, etc.

- **Inject.NET.Tests** - Integration/runtime tests
  - Uses TUnit framework with async/await patterns
  - Tests cover: singletons, scoped, transient, decorators, generics, tenancy, circular dependencies
  - `EmitCompilerGeneratedFiles` enabled - outputs to `SourceGeneratedViewer/` for inspection

- **Inject.NET.SourceGenerator.Tests** - Generator unit tests
  - Uses Verify snapshot testing with `.verified.txt` files
  - `TestsBase<TGenerator>` - Base class that drives the generator and compares output

- **Inject.NET.SourceGenerator.Sample** - Sample code for generator tests

- **Inject.NET.Pipeline** - Additional utilities (purpose TBD)

- **Benchmarks** - BenchmarkDotNet comparisons with other DI containers (Autofac, DryIoc, MS.DI, etc.)

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

# Run a single test by full method name
dotnet test --filter "FullyQualifiedName~Inject.NET.Tests.Singletons.SameInstanceWhenResolvingMultipleTimes_FromSameScope"
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
- `[Singleton]`, `[Scoped]`, `[Transient]` - Define service lifetimes (supports generic and non-generic forms)
- `[WithTenant]` - Enables multi-tenancy support
- `[Decorator]`, `[SingletonDecorator]`, `[ScopedDecorator]`, `[TransientDecorator]` - Apply decorator pattern to services
- `[ServiceKey]` - Register keyed services for disambiguation

### Advanced Features
- **Open Generics**: Services can be registered as open generic types (e.g., `IRepository<>` implemented by `Repository<>`)
- **Decorators**: Multiple decorators can wrap services with configurable order via the `Order` property
- **Multi-Tenancy**: Tenant-specific service instances with `[WithTenant<T>]` and optional overriding types
- **Keyed Services**: Multiple implementations of same interface distinguished by keys

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

- Core library (Inject.NET) targets both .NET 8.0 and .NET 9.0
- Test projects and samples use .NET 9.0 with C# preview features
- TUnit is the test framework (not xUnit/NUnit)
- Source generators require rebuilding the Inject.NET.SourceGenerator project to see changes
- Verify snapshot tests auto-generate `.verified.txt` files - these are committed to source control
- When snapshot tests fail, review the `.received.txt` files to see actual vs expected output