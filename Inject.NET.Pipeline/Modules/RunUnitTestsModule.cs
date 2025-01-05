using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Inject.NET.Pipeline.Modules;

public class RunUnitTestsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return
        [
            await context.DotNet()
                .Test(new DotNetTestOptions(Sourcy.DotNet.Projects.Inject_NET_SourceGenerator_Tests.FullName),
                    cancellationToken)
        ];
    }
}
