using Microsoft.Extensions.DependencyInjection;

namespace InterfaceAggregate;

public sealed class ServiceCollectionFacadeStrategy : IFacadeRegistrationStrategy
{
    private readonly IServiceCollection _services;
    private readonly ServiceLifetime _lifetime;

    public ServiceCollectionFacadeStrategy(IServiceCollection services, ServiceLifetime lifetime)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _lifetime = lifetime;
    }

    public void RegisterServices(Type facadeType, IImplementationTypeResolver resolver)
    {
        var properties = facadeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var implementationType = resolver.ResolveImplementationType(property.PropertyType);
            _services.Add(new ServiceDescriptor(property.PropertyType, implementationType, _lifetime));
        }

        _services.Add(new ServiceDescriptor(
            facadeType,
            sp => CreateFacadeInstance(facadeType, properties, sp),
            _lifetime));
    }

    private static object CreateFacadeInstance(
        Type facadeType,
        IEnumerable<PropertyInfo> properties,
        IServiceProvider serviceProvider)
    {
        var proxy = DynamicProxyFactory.Create(facadeType);


        if (proxy is DynamicFacadeProxy facadeProxy)
        {
            foreach (var property in properties)
            {
                var service = serviceProvider.GetRequiredService(property.PropertyType);
                facadeProxy.SetProperty(property.PropertyType, property.Name, service);
            }
        }

        return proxy;
    }
}