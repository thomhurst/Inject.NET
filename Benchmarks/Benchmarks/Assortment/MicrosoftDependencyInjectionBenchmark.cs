using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks.Benchmarks.Assortment;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Assortment")]
public class MicrosoftDependencyInjectionBenchmark
{
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton<Interface1, Class1>()
            .AddSingleton<Interface2, Class2>()
            .AddSingleton<Interface3, Class3>()
            .AddTransient<Interface4, Class4>()
            .AddScoped<Interface5, Class5>()
            .BuildServiceProvider();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<Interface5>();
    }
}