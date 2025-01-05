using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
public class InjectDotNetBenchmark
{
    [ServiceProvider]
    [Singleton<Interface1, Class1>]
    [Singleton<Interface2, Class2>]
    [Singleton<Interface3, Class3>]
    [Singleton<Interface4, Class4>]
    [Singleton<Interface5, Class5>]
    public class MyServiceProvider;
    
    private IServiceProviderRoot _serviceProviderRoot;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _serviceProviderRoot = await Benchmarks.MyServiceProvider.BuildAsync();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProviderRoot.CreateScope();

        scope.GetRequiredService<Interface5>();
    }
}