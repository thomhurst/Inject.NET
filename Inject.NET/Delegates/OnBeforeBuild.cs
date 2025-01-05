using Inject.NET.Interfaces;

namespace Inject.NET.Delegates;

public delegate void OnBeforeBuild(IServiceRegistrar serviceRegistrar);
public delegate void OnBeforeTenantBuild(ITenantedServiceRegistrar serviceRegistrar);