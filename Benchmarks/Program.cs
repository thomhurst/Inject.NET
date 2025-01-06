using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;
using Benchmarks.Benchmarks.Singleton;
using InjectDotNetBenchmark = Benchmarks.Benchmarks.ScopedWithInterface.InjectDotNetBenchmark;

var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.JoinSummary);

BenchmarkRunner.Run([
    BenchmarkConverter.TypeToBenchmarks(typeof(MicrosoftDependencyInjectionBenchmark), config),
    BenchmarkConverter.TypeToBenchmarks(typeof(InjectDotNetBenchmark), config),
    BenchmarkConverter.TypeToBenchmarks(typeof(NinjectBenchmark), config),
    BenchmarkConverter.TypeToBenchmarks(typeof(AutofacBenchmark), config),
]);

var output = new DirectoryInfo(Environment.CurrentDirectory)
    .GetFiles("*.md", SearchOption.AllDirectories)
    .OrderBy(x => x.Name)
    .First();

var file = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");

if (!string.IsNullOrEmpty(file))
{
    await File.WriteAllTextAsync(file, await File.ReadAllTextAsync(output.FullName));
}