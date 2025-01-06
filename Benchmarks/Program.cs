using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;
using Benchmarks.Benchmarks.Singleton;
using InjectDotNetBenchmark = Benchmarks.Benchmarks.ScopedWithInterface.InjectDotNetBenchmark;

var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.JoinSummary);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

var output = new DirectoryInfo(Environment.CurrentDirectory)
    .GetFiles("*.md", SearchOption.AllDirectories)
    .OrderBy(x => x.Name)
    .First();

var file = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");

if (!string.IsNullOrEmpty(file))
{
    await File.WriteAllTextAsync(file, await File.ReadAllTextAsync(output.FullName));
}