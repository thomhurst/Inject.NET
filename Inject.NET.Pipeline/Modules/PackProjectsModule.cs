using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;

namespace Inject.NET.Pipeline.Modules;

[DependsOn<PackageFilesRemovalModule>]
[DependsOn<NugetVersionGeneratorModule>]
[DependsOn<RunUnitTestsModule>]
public class PackProjectsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packageVersion = await context.GetModule<NugetVersionGeneratorModule>();

        return
        [
            await context.Tools.DotNet.PackAsync(new DotNetPackOptions
            {
                ProjectSolution = Sourcy.DotNet.Projects.Inject_NET.FullName,
                Configuration = "Release",
                Properties =
                [
                    ("PackageVersion", packageVersion.Value),
                    ("Version", packageVersion.Value)
                ],
                IncludeSource = true
            }, cancellationToken: cancellationToken)
        ];
    }
}
