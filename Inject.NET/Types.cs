using Inject.NET.Interfaces;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET;

public class Types
{
    public static readonly Type ServiceScope = typeof(IServiceScope);
    public static readonly Type ServiceProvider = typeof(IServiceProvider);
    public static readonly Type SystemServiceProvider = typeof(System.IServiceProvider);
}