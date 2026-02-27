namespace Inject.NET.Models;

/// <summary>
/// Base class for parameter overrides used when resolving services.
/// Allows providing runtime values for constructor parameters that would
/// normally be resolved from the container.
/// </summary>
public abstract class Parameter
{
    /// <summary>
    /// Attempts to match this parameter override against a constructor parameter.
    /// </summary>
    /// <param name="parameterType">The type of the constructor parameter</param>
    /// <param name="parameterName">The name of the constructor parameter</param>
    /// <param name="value">The override value if matched</param>
    /// <returns>True if this parameter override matches the constructor parameter</returns>
    public abstract bool TryMatch(Type parameterType, string parameterName, out object? value);
}

/// <summary>
/// A parameter override that matches constructor parameters by type.
/// When resolving a service, any constructor parameter of type T will
/// receive the specified value instead of being resolved from the container.
/// </summary>
/// <typeparam name="T">The type of the parameter to override</typeparam>
public class TypedParameter<T> : Parameter
{
    private readonly T _value;

    /// <summary>
    /// Creates a new typed parameter override.
    /// </summary>
    /// <param name="value">The value to use for constructor parameters of type T</param>
    public TypedParameter(T value)
    {
        _value = value;
    }

    /// <inheritdoc />
    public override bool TryMatch(Type parameterType, string parameterName, out object? value)
    {
        if (parameterType == typeof(T))
        {
            value = _value;
            return true;
        }

        value = null;
        return false;
    }
}

/// <summary>
/// A parameter override that matches constructor parameters by name.
/// When resolving a service, any constructor parameter with the specified
/// name will receive the provided value instead of being resolved from the container.
/// </summary>
public class NamedParameter : Parameter
{
    private readonly string _name;
    private readonly object? _value;

    /// <summary>
    /// Creates a new named parameter override.
    /// </summary>
    /// <param name="name">The constructor parameter name to match</param>
    /// <param name="value">The value to use for the matched parameter</param>
    public NamedParameter(string name, object? value)
    {
        _name = name;
        _value = value;
    }

    /// <inheritdoc />
    public override bool TryMatch(Type parameterType, string parameterName, out object? value)
    {
        if (string.Equals(_name, parameterName, StringComparison.Ordinal))
        {
            value = _value;
            return true;
        }

        value = null;
        return false;
    }
}
