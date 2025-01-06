using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks.Benchmarks.Scoped;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Scoped")]
public class MicrosoftDependencyInjectionBenchmark
{
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new ServiceCollection()
            .AddScoped<Class1>()
            .BuildServiceProvider();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<Class1>();
    }
}