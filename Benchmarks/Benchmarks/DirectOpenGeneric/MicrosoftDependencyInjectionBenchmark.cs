﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks.Benchmarks.DirectOpenGeneric;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("DirectOpenGeneric")]
public class MicrosoftDependencyInjectionBenchmark
{
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new ServiceCollection()
            .AddTransient<Class1>()
            .AddTransient(typeof(IGenericInterface<>), typeof(GenericClass<>))
            .BuildServiceProvider();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IGenericInterface<Class1>>();
    }
}