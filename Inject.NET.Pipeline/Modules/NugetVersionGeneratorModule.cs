using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;

// ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162

namespace Inject.NET.Pipeline.Modules;

public class NugetVersionGeneratorModule : Module<string>
{
    protected override async Task<string> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        try
        {
            var gitVersionInformation = await context.Tools.Git.Versioning.GetVersioningInformationAsync(cancellationToken);

            if (gitVersionInformation.BranchName == "main")
            {
                return gitVersionInformation.SemVer!;
            }

            return $"{gitVersionInformation.Major}.{gitVersionInformation.Minor}.{gitVersionInformation.Patch}-{gitVersionInformation.PreReleaseLabel}-{gitVersionInformation.CommitsSinceVersionSource}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            context.Logger.LogWarning(ex, "GitVersion failed — using fallback version for PR build");
            return "0.0.0-pr-fallback";
        }
    }

    protected override Task<ModuleResult<string>?> OnAfterExecuteAsync(IModuleContext context, ModuleResult<string> result, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("NuGet Version to Package: {Version}", result.ValueOrDefault);
        return Task.FromResult<ModuleResult<string>?>(null);
    }
}
