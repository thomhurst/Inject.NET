using System;
using System.Threading.Tasks;
using Inject.NET.Extensions;
using Inject.NET.SourceGenerator.Sample.Models;

namespace Inject.NET.SourceGenerator.Sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var serviceProvider = await MyServiceProvider.BuildAsync();

        var @class = serviceProvider.CreateScope().GetRequiredService<Class5>();
        
        Console.WriteLine(@class);
    }
}