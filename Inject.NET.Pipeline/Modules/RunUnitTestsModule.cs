using EnumerableAsyncProcessor.Extensions;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;

namespace Inject.NET.Pipeline.Modules;

public class RunUnitTestsModule : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        string[] projects =
        [
            Sourcy.DotNet.Projects.Inject_NET_SourceGenerator_Tests.FullName,
            Sourcy.DotNet.Projects.Inject_NET_Tests.FullName
        ];

        return await projects.SelectAsync(project => context.Tools.DotNet.RunAsync(new DotNetRunOptions
            {
                Project = project
            }, cancellationToken: cancellationToken),
            cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}
