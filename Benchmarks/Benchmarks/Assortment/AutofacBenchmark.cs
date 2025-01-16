using Autofac;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks.Models;

namespace Benchmarks.Benchmarks.Assortment;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90)]
[BenchmarkCategory("Assortment")]
public class AutofacBenchmark
{
    private IContainer _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterType<Class1>().As<Interface1>().SingleInstance();
        containerBuilder.RegisterType<Class2>().As<Interface2>().SingleInstance();
        containerBuilder.RegisterType<Class3>().As<Interface3>().SingleInstance();
        containerBuilder.RegisterType<Class4>().As<Interface4>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<Class5>().As<Interface5>().InstancePerDependency();


        _serviceProvider = containerBuilder.Build();
    }

    [Benchmark]
    public async Task GetDependency()
    {
        await using var scope = _serviceProvider.BeginLifetimeScope();

        scope.Resolve<Interface5>();
    }
}