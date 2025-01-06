﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;
using Ninject;

namespace Benchmarks.Benchmarks.Assortment;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Assortment")]
public class NinjectBenchmark
{
    private IKernel _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _serviceProvider = new StandardKernel()
            .Bind<Interface1>().To<Class1>().InSingletonScope()
            .Kernel!.Bind<Interface2>().To<Class2>().InSingletonScope()
            .Kernel!.Bind<Interface3>().To<Class3>().InSingletonScope()
            .Kernel!.Bind<Interface4>().To<Class4>().InTransientScope()
            .Kernel!.Bind<Interface5>().To<Class5>().InThreadScope()
            .Kernel!;
    }

    [Benchmark]
    public void GetDependency()
    {
        using var scope = _serviceProvider.BeginBlock();

        scope.Get<Interface5>();
    }
}