using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.DependencyInjection.Tests;

public partial class ServiceProviderIsServiceAdapterTests
{
    public class RegisteredSingleton;

    public class UnregisteredType;

    [ServiceProvider]
    [Singleton<RegisteredSingleton>]
    public partial class IsServiceTestProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddServiceCollection(_ => { });
            }
        }
    }

    [Test]
    public async Task IServiceProviderIsService_CanBeResolvedFromScope()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var checker = scope.GetRequiredService<IServiceProviderIsService>();

        await Assert.That(checker).IsNotNull();
    }

    [Test]
    public async Task IsService_ReturnsTrueForRegisteredSingleton()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var checker = scope.GetRequiredService<IServiceProviderIsService>();

        await Assert.That(checker.IsService(typeof(RegisteredSingleton))).IsTrue();
    }

    [Test]
    public async Task IsService_ReturnsFalseForUnregisteredType()
    {
        await using var serviceProvider = await IsServiceTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var checker = scope.GetRequiredService<IServiceProviderIsService>();

        await Assert.That(checker.IsService(typeof(UnregisteredType))).IsFalse();
    }
}
