using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks.Benchmarks.OpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("OpenGeneric")]
public class MicrosoftDependencyInjectionBenchmark
{
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new ServiceCollection()
            .AddTransient<Class1>()
            .AddTransient<GenericWrapper>()
            .AddTransient(typeof(IGenericInterface<>), typeof(GenericClass<>))
            .BuildServiceProvider();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<GenericWrapper>();
    }
}