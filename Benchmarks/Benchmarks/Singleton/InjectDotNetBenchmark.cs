using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;

namespace Benchmarks.Benchmarks.Singleton;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Singleton")]
public partial class InjectDotNetBenchmark
{
    [ServiceProvider]
    [Singleton<Class1>]
    public partial class MyServiceProvider;
    
    private MyServiceProvider.ServiceProvider_ _serviceProviderRoot = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _serviceProviderRoot = await MyServiceProvider.BuildAsync();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProviderRoot.CreateScope();

        scope.GetRequiredService<Class1>();
    }
}