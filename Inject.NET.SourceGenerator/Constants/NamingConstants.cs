namespace Inject.NET.SourceGenerator.Constants;

/// <summary>
/// Contains constants used for naming patterns and string transformations.
/// </summary>
public static class NamingConstants
{
    /// <summary>
    /// Characters and patterns used in type name transformations.
    /// </summary>
    public static class TypeNameReplacements
    {
        /// <summary>
        /// Replacement for generic type angle brackets (&lt; and &gt;).
        /// </summary>
        public const string AngleBracketReplacement = "_";

        /// <summary>
        /// Replacement for namespace dots (.).
        /// </summary>
        public const string DotReplacement = "__";

        /// <summary>
        /// Replacement for commas (,).
        /// </summary>
        public const string CommaReplacement = "_";

        /// <summary>
        /// Replacement for spaces ( ).
        /// </summary>
        public const string SpaceReplacement = "_";

        /// <summary>
        /// Nullable type suffix (?) is removed entirely.
        /// </summary>
        public const string NullableReplacement = "";
    }

    /// <summary>
    /// Separators used in generated property and field names.
    /// </summary>
    public static class Separators
    {
        /// <summary>
        /// Main separator used between type, tenant, and index components.
        /// </summary>
        public const string MainSeparator = "__";

        /// <summary>
        /// Prefix used for keyed services.
        /// </summary>
        public const string KeyedPrefix = "Keyed__";
    }

    /// <summary>
    /// Field naming conventions.
    /// </summary>
    public static class FieldNaming
    {
        /// <summary>
        /// Prefix for private field names.
        /// </summary>
        public const string FieldPrefix = "_";
    }
}