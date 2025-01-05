using System;
using Inject.NET.SourceGenerator.Sample.Models;
using Inject.NET.SourceGenerator.Sample.ServiceProviders;

var serviceProvider = await SingletonGeneric2.BuildAsync();

var typedGeneric = serviceProvider.CreateScope().GetService(typeof(IGeneric<Class1>))!;

Console.WriteLine(typedGeneric);