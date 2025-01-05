namespace Inject.NET.Interfaces;

public interface ISingletonAsyncInitialization
{
    Task InitializeAsync();
    int Order => 0;
}