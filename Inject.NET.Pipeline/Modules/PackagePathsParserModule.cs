using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace Inject.NET.Pipeline.Modules;

[DependsOn<PackProjectsModule>]
public class PackagePathsParserModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var packPackagesModuleResult = await GetModule<PackProjectsModule>();

        return packPackagesModuleResult.Value!
            .Select(x => x.StandardOutput)
            .Select(x => x.Split("Successfully created package '")[1])
            .Select(x => x.Split("'.")[0])
            .Select(x => new File(x))
            .ToList();
    }
}
