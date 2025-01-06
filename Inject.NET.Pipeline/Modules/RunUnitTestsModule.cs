using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Inject.NET.Pipeline.Modules;

public class RunUnitTestsModule : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        string[] projects =
        [
            Sourcy.DotNet.Projects.Inject_NET_SourceGenerator_Tests.FullName,
            Sourcy.DotNet.Projects.Inject_NET_Tests.FullName
        ];

        return await projects.SelectAsync(x => context.DotNet().Test(new DotNetTestOptions(x), cancellationToken),
            cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}
