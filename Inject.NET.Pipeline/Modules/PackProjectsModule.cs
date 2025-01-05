using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Inject.NET.Pipeline.Modules;

[DependsOn<PackageFilesRemovalModule>]
[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<RunUnitTestsModule>]
public class PackProjectsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var packageVersion = await GetModule<NugetVersionGeneratorModule>();

        return
        [
            await context.DotNet().Pack(new DotNetPackOptions
            {
                ProjectSolution = Sourcy.DotNet.Projects.Inject_NET.FullName,
                Configuration = Configuration.Release,
                Properties =
                [
                    ("PackageVersion", packageVersion.Value)!,
                    ("Version", packageVersion.Value)!
                ],
                IncludeSource = true
            }, cancellationToken)
        ];
    }
}