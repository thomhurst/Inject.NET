using Inject.NET.Attributes;
using Inject.NET.Extensions;
using Inject.NET.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inject.NET.Extensions.DependencyInjection.Tests;

public partial class OptionsIntegrationTests
{
    public class MyOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [ServiceProvider]
    public partial class OptionsTestProvider
    {
        public partial class ServiceRegistrar_
        {
            partial void ConfigureServices()
            {
                this.AddServiceCollection(services =>
                {
                    services.AddOptions<MyOptions>().Configure(o => o.Value = "configured");
                });
            }
        }
    }

    [Test]
    public async Task Options_CanBeResolvedWithConfiguredValue()
    {
        await using var serviceProvider = await OptionsTestProvider.BuildAsync();
        await using var scope = serviceProvider.CreateScope();

        var options = scope.GetRequiredService<IOptions<MyOptions>>();

        await Assert.That(options).IsNotNull();
        await Assert.That(options.Value.Value).IsEqualTo("configured");
    }
}
