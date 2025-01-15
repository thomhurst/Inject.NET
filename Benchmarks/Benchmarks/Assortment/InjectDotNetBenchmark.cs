using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;

namespace Benchmarks.Benchmarks.Assortment;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Assortment")]
public partial class InjectDotNetBenchmark
{
    [ServiceProvider]
    [Singleton<Interface1, Class1>]
    [Singleton<Interface2, Class2>]
    [Singleton<Interface3, Class3>]
    [Transient<Interface4, Class4>]
    [Scoped<Interface5, Class5>]
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

        scope.GetRequiredService<Interface5>();
    }
}