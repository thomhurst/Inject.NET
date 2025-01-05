using System;
using Inject.NET.SourceGenerator.Sample.Models;
using Inject.NET.SourceGenerator.Sample.ServiceProviders;

var serviceProvider = await SingletonGeneric.BuildAsync();

var typedGeneric = (Generic<Class1>) serviceProvider.CreateScope().GetService(typeof(Generic<Class1>))!;

Console.WriteLine(typedGeneric);