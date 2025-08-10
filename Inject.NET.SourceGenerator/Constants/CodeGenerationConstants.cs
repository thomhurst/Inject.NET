namespace Inject.NET.SourceGenerator.Constants;

/// <summary>
/// Contains constants used for code generation patterns and naming conventions.
/// </summary>
public static class CodeGenerationConstants
{
    /// <summary>
    /// Class name suffixes used in generated code.
    /// </summary>
    public static class ClassSuffixes
    {
        /// <summary>
        /// Suffix for generated service provider classes.
        /// </summary>
        public const string ServiceProvider = "ServiceProvider_";

        /// <summary>
        /// Suffix for generated service scope classes.
        /// </summary>
        public const string ServiceScope = "ServiceScope_";

        /// <summary>
        /// Suffix for generated singleton scope classes.
        /// </summary>
        public const string SingletonScope = "SingletonScope_";

        /// <summary>
        /// Suffix for generated service registrar classes.
        /// </summary>
        public const string ServiceRegistrar = "ServiceRegistrar_";

        /// <summary>
        /// Prefix for tenant-specific classes.
        /// </summary>
        public const string TenantPrefix = "Tenant_";
    }

    /// <summary>
    /// Namespace prefixes for global type references.
    /// </summary>
    public static class GlobalNamespaces
    {
        /// <summary>
        /// Global reference to the Inject.NET namespace.
        /// </summary>
        public const string InjectNet = "global::Inject.NET.";

        /// <summary>
        /// Global reference to Inject.NET Services namespace.
        /// </summary>
        public const string Services = "Services.";

        /// <summary>
        /// Global reference to Inject.NET Models namespace.
        /// </summary>
        public const string Models = "Models.";

        /// <summary>
        /// Global reference to Inject.NET Interfaces namespace.
        /// </summary>
        public const string Interfaces = "Interfaces.";
    }

    /// <summary>
    /// Common method names used in generated code.
    /// </summary>
    public static class MethodNames
    {
        /// <summary>
        /// Async initialization method name.
        /// </summary>
        public const string InitializeAsync = "InitializeAsync";

        /// <summary>
        /// Method name for creating typed scopes.
        /// </summary>
        public const string CreateTypedScope = "CreateTypedScope";

        /// <summary>
        /// Method name for building service providers.
        /// </summary>
        public const string BuildAsync = "BuildAsync";
    }

    /// <summary>
    /// Property names used in generated code.
    /// </summary>
    public static class PropertyNames
    {
        /// <summary>
        /// Property name for accessing singletons.
        /// </summary>
        public const string Singletons = "Singletons";

        /// <summary>
        /// Property name for service factories.
        /// </summary>
        public const string ServiceFactories = "serviceFactories";

        /// <summary>
        /// Property name for service key.
        /// </summary>
        public const string Key = "Key";
    }

    /// <summary>
    /// Parameter names commonly used in generated constructors and methods.
    /// </summary>
    public static class ParameterNames
    {
        /// <summary>
        /// Parameter name for service provider instances.
        /// </summary>
        public const string ServiceProvider = "serviceProvider";

        /// <summary>
        /// Parameter name for service factories.
        /// </summary>
        public const string ServiceFactories = "serviceFactories";

        /// <summary>
        /// Parameter name for parent scope instances.
        /// </summary>
        public const string ParentScope = "parentScope";

        /// <summary>
        /// Parameter name for parent service provider instances.
        /// </summary>
        public const string ParentServiceProvider = "parentServiceProvider";
    }
}