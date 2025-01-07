using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;

namespace Benchmarks.Benchmarks.UncachedOpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("UncachedOpenGeneric")]
public partial class InjectDotNetBenchmark
{
    [ServiceProvider]
    [Transient(typeof(IGenericInterface<>), typeof(GenericClass<>))]
    [Transient<Class1>]
    public partial class MyServiceProvider;
    
    private IServiceProviderRoot _serviceProviderRoot = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _serviceProviderRoot = await MyServiceProvider.BuildAsync();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProviderRoot.CreateScope();

        scope.GetRequiredService<IGenericInterface<Class1>>();
    }
}