using Microsoft.Extensions.DependencyInjection;

namespace InterfaceAggregate.Interfaces;

public interface IFacadeRegistrar
{
    void RegisterAggregat(ServiceLifetime lifetime = ServiceLifetime.Scoped);
}

public interface IFacadeRegistrationStrategy
{
    void RegisterServices(Type facadeType, IImplementationTypeResolver resolver);
}

public interface IImplementationTypeResolver
{
    Type ResolveImplementationType(Type interfaceType);
}

public interface IPropertyValidator
{
    void ValidateProperties(IEnumerable<PropertyInfo> properties, Type facadeType);
}