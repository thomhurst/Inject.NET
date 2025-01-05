﻿using Inject.NET.Attributes;

namespace Inject.NET.SourceGenerator.Sample.ServiceProviders;

[Singleton<Interface1, Class1>]
[ServiceProvider]
public partial class SingletonServiceImplementation;