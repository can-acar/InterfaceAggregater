using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace InterfaceAggregate;

/// <summary>
/// Represents a dynamic proxy that handles method invocations for facade interfaces.
/// </summary>
public class DynamicFacadeProxy : DispatchProxy
{
    private readonly ImplementationDictionary _implementations = new();

    //ConcurrentDictionary<string, object> _implementations = new();
    private Type _facadeType;

    /// <summary>
    /// Initializes the proxy with the specified facade type.
    /// </summary>
    /// <param name="facadeType">The type of the facade interface.</param>
    /// <exception cref="ArgumentNullException">Thrown when facadeType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when facadeType is not an interface.</exception>
    internal void Initialize(Type facadeType)
    {
        ArgumentNullException.ThrowIfNull(facadeType);

        if (!facadeType.IsInterface)
        {
            throw new ArgumentException("Facade type must be an interface", nameof(facadeType));
        }

        _facadeType = facadeType;
    }

    /// <summary>
    /// Sets the implementation for a specific property.
    /// </summary>
    /// <param name="propertyType"></param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="implementation">The implementation object.</param>
    /// <exception cref="ArgumentNullException">Thrown when propertyName or implementation is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is empty or whitespace.</exception>
    internal void SetProperty(Type propertyType, string propertyName, object implementation)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(implementation);

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be empty or whitespace", nameof(propertyName));
        }

        _implementations.AddOrUpdate(propertyType, propertyName, implementation);
    }

    /// <summary>
    /// Invokes the specified method on the proxy.
    /// </summary>
    /// <param name="targetMethod">The method to invoke.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when targetMethod is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the method is not a property getter.</exception>
    /// <exception cref="InvalidOperationException">Thrown when implementation is not found.</exception>
    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        ArgumentNullException.ThrowIfNull(args);

        ValidateMethod(targetMethod);

        var propertyName = ExtractPropertyName(targetMethod.Name);

        return _implementations.TryGetValue(propertyName, out var implementation)
            ? implementation
            : throw new InvalidOperationException(
                $"Implementation not found for property '{propertyName}' in facade '{_facadeType?.Name}'");
    }

    private static void ValidateMethod(MethodInfo method)
    {
        if (!method.IsSpecialName || !method.Name.StartsWith("get_"))
        {
            throw new NotSupportedException(
                $"Method '{method.Name}' is not supported. Only property getters are supported.");
        }
    }

    private static string ExtractPropertyName(string methodName)
    {
        return methodName[4..];
    }
}

/// <summary>
/// Represents a thread-safe dictionary for managing implementations of facade properties across different types.
/// </summary>
internal class ImplementationDictionary : ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>
{
    /// <summary>
    /// Adds an implementation for a specific type and property.
    /// </summary>
    /// <param name="type">The type of the facade.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="implementation">The implementation object.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is empty or whitespace.</exception>
    public void AddOrUpdate(Type type, string propertyName, object implementation)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(implementation);

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be empty or whitespace", nameof(propertyName));
        }

        var typeDict = GetOrAdd(type, _ => new ConcurrentDictionary<string, object>(StringComparer.Ordinal));
        typeDict.AddOrUpdate(propertyName, implementation, (_, _) => implementation);
    }

    /// <summary>
    /// Attempts to get the implementation for a specific property across all types.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="implementation">The implementation object if found; otherwise, null.</param>
    /// <returns>True if the implementation was found; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when propertyName is null.</exception>
    public bool TryGetValue(string propertyName, out object implementation)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        foreach (var typeDict in Values)
        {
            if (typeDict.TryGetValue(propertyName, out implementation))
            {
                return true;
            }
        }

        implementation = null;
        return false;
    }
}