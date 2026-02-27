using Inject.NET.Attributes;
using Inject.NET.Extensions;

namespace Inject.NET.Tests;

public partial class ExternallyOwnedTests
{
    [Test]
    public async Task ExternallyOwned_Singleton_IsNotDisposed_WhenProviderIsDisposed()
    {
        DisposableSingleton instance;

        var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        await using (var scope = serviceProvider.CreateScope())
        {
            instance = scope.GetRequiredService<DisposableSingleton>();
            await Assert.That(instance.IsDisposed).IsFalse();
        }

        await serviceProvider.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsFalse();
    }

    [Test]
    public async Task ExternallyOwned_Scoped_IsNotDisposed_WhenScopeIsDisposed()
    {
        DisposableScoped instance;

        await using var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        var scope = serviceProvider.CreateScope();
        instance = scope.GetRequiredService<DisposableScoped>();

        await Assert.That(instance.IsDisposed).IsFalse();

        await scope.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsFalse();
    }

    [Test]
    public async Task ExternallyOwned_Transient_IsNotDisposed_WhenScopeIsDisposed()
    {
        DisposableTransient instance;

        await using var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        var scope = serviceProvider.CreateScope();
        instance = scope.GetRequiredService<DisposableTransient>();

        await Assert.That(instance.IsDisposed).IsFalse();

        await scope.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsFalse();
    }

    [Test]
    public async Task NonExternallyOwned_Singleton_IsDisposed_WhenProviderIsDisposed()
    {
        ManagedDisposableSingleton instance;

        var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        await using (var scope = serviceProvider.CreateScope())
        {
            instance = scope.GetRequiredService<ManagedDisposableSingleton>();
            await Assert.That(instance.IsDisposed).IsFalse();
        }

        await serviceProvider.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsTrue();
    }

    [Test]
    public async Task NonExternallyOwned_Scoped_IsDisposed_WhenScopeIsDisposed()
    {
        ManagedDisposableScoped instance;

        await using var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        var scope = serviceProvider.CreateScope();
        instance = scope.GetRequiredService<ManagedDisposableScoped>();

        await Assert.That(instance.IsDisposed).IsFalse();

        await scope.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsTrue();
    }

    [Test]
    public async Task NonExternallyOwned_Transient_IsDisposed_WhenScopeIsDisposed()
    {
        ManagedDisposableTransient instance;

        await using var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        var scope = serviceProvider.CreateScope();
        instance = scope.GetRequiredService<ManagedDisposableTransient>();

        await Assert.That(instance.IsDisposed).IsFalse();

        await scope.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsTrue();
    }

    [Test]
    public async Task ExternallyOwned_AsyncDisposable_Singleton_IsNotDisposed_WhenProviderIsDisposed()
    {
        AsyncDisposableSingleton instance;

        var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        await using (var scope = serviceProvider.CreateScope())
        {
            instance = scope.GetRequiredService<AsyncDisposableSingleton>();
            await Assert.That(instance.IsDisposed).IsFalse();
        }

        await serviceProvider.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsFalse();
    }

    [Test]
    public async Task NonExternallyOwned_AsyncDisposable_Singleton_IsDisposed_WhenProviderIsDisposed()
    {
        ManagedAsyncDisposableSingleton instance;

        var serviceProvider = await ExternallyOwnedServiceProvider.BuildAsync();

        await using (var scope = serviceProvider.CreateScope())
        {
            instance = scope.GetRequiredService<ManagedAsyncDisposableSingleton>();
            await Assert.That(instance.IsDisposed).IsFalse();
        }

        await serviceProvider.DisposeAsync();

        await Assert.That(instance.IsDisposed).IsTrue();
    }

    // --- Service types ---

    public class DisposableSingleton : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class DisposableScoped : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class DisposableTransient : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class AsyncDisposableSingleton : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    public class ManagedDisposableSingleton : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class ManagedDisposableScoped : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class ManagedDisposableTransient : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class ManagedAsyncDisposableSingleton : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    // --- Service Provider ---

    [ServiceProvider]
    [Singleton<DisposableSingleton>(ExternallyOwned = true)]
    [Scoped<DisposableScoped>(ExternallyOwned = true)]
    [Transient<DisposableTransient>(ExternallyOwned = true)]
    [Singleton<AsyncDisposableSingleton>(ExternallyOwned = true)]
    [Singleton<ManagedDisposableSingleton>]
    [Scoped<ManagedDisposableScoped>]
    [Transient<ManagedDisposableTransient>]
    [Singleton<ManagedAsyncDisposableSingleton>]
    public partial class ExternallyOwnedServiceProvider;
}
