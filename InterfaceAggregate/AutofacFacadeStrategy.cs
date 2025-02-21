using Autofac;
using Castle.Core.Logging;

namespace InterfaceAggregate;

public sealed class AutofacFacadeStrategy : IFacadeRegistrationStrategy
{
    private readonly ContainerBuilder _builder;
    private readonly ILogger _logger;

    public AutofacFacadeStrategy(ContainerBuilder builder, ILogger logger = null)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _logger = logger ?? NullLogger.Instance;
    }

    public void RegisterServices(Type facadeType, IImplementationTypeResolver resolver)
    {
        var properties = facadeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        RegisterDependencies(properties, resolver);
        RegisterFacade(facadeType, properties);
    }

    private void RegisterDependencies(IEnumerable<PropertyInfo> properties, IImplementationTypeResolver resolver)
    {
        foreach (var property in properties)
        {
            var implementationType = resolver.ResolveImplementationType(property.PropertyType);
            _builder.RegisterType(implementationType)
                .As(property.PropertyType)
                .InstancePerLifetimeScope();
        }
    }

    private void RegisterFacade(Type facadeType, IEnumerable<PropertyInfo> properties)
    {
        _builder.Register(context =>
            {
                try
                {
                    var proxy = DynamicProxyFactory.Create(facadeType);

                    if (proxy is DynamicFacadeProxy facadeProxy)
                    {
                        foreach (var property in properties)
                        {
                            var service = context.Resolve(property.PropertyType);
                            facadeProxy.SetProperty(property.PropertyType, property.Name, service);
                        }
                    }

                    return proxy;
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to create facade for type {FacadeType}", ex);
                    throw;
                }
            })
            .As(facadeType)
            .InstancePerLifetimeScope();
    }
}