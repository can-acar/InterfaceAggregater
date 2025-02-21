using System;
using System.Reflection;
using System.Linq;

namespace InterfaceAggregate;

/// <summary>
/// Factory class for creating dynamic proxies using DispatchProxy.
/// This class handles the creation of proxy objects for interface implementations.
/// </summary>
internal static class DynamicProxyFactory
{
    private static readonly MethodInfo CreateMethodInfo;

    /// <summary>
    /// Static constructor to initialize the CreateMethodInfo field.
    /// Throws TypeLoadException if the required DispatchProxy.Create method cannot be found.
    /// </summary>
    static DynamicProxyFactory()
    {
        try
        {
            CreateMethodInfo = typeof(DispatchProxy)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == nameof(DispatchProxy.Create) &&
                                   m.GetGenericArguments().Length == 2)
                ?? throw new TypeLoadException("DispatchProxy.Create method not found");
        }
        catch (Exception ex)
        {
            throw new TypeLoadException(
                "Failed to initialize DynamicProxyFactory. This is a critical error.", ex);
        }
    }

    /// <summary>
    /// Creates a dynamic proxy instance for the specified facade type.
    /// </summary>
    /// <param name="facadeType">The type of facade to create a proxy for.</param>
    /// <returns>The created proxy object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when facadeType is null.</exception>
    /// <exception cref="FacadeRegistrationException">Thrown when proxy creation fails.</exception>
    public static object Create(Type facadeType)
    {
        if (facadeType == null)
        {
            throw new ArgumentNullException(nameof(facadeType));
        }

        try
        {
            ValidateFacadeType(facadeType);

            var genericMethod = CreateMethodInfo.MakeGenericMethod(facadeType, typeof(DynamicFacadeProxy));
            var proxy = genericMethod.Invoke(null, null)
                ?? throw new FacadeRegistrationException($"Proxy creation returned null for type {facadeType.Name}");

            if (proxy is DynamicFacadeProxy facadeProxy)
            {
                facadeProxy.Initialize(facadeType);
            }
            else
            {
                throw new FacadeRegistrationException(
                    $"Created proxy is not of expected type DynamicFacadeProxy for {facadeType.Name}");
            }

            return proxy;
        }
        catch (Exception ex) when (ex is not FacadeRegistrationException)
        {
            throw new FacadeRegistrationException(
                $"Failed to create proxy for type {facadeType.Name}.", ex);
        }
    }

    /// <summary>
    /// Validates the facade type meets all requirements for proxy creation.
    /// </summary>
    /// <param name="facadeType">The type to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the type doesn't meet requirements.</exception>
    private static void ValidateFacadeType(Type facadeType)
    {
        if (!facadeType.IsInterface)
        {
            throw new ArgumentException("Facade type must be an interface", nameof(facadeType));
        }

        if (facadeType.IsGenericTypeDefinition)
        {
            throw new ArgumentException("Generic type definitions are not supported", nameof(facadeType));
        }
    }
}