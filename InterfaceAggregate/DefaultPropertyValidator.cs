namespace InterfaceAggregate;

public sealed class DefaultPropertyValidator : IPropertyValidator
{
    public void ValidateProperties(IEnumerable<PropertyInfo> properties, Type facadeType)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(facadeType);

        var propertyList = properties.ToList();

        if (!propertyList.Any())
        {
            throw new FacadeRegistrationException($"Facade type {facadeType.Name} must have at least one property");
        }

        foreach (var property in propertyList)
        {
            ValidateProperty(property, facadeType);
        }
    }

    private static void ValidateProperty(PropertyInfo property, Type facadeType)
    {
        if (!property.PropertyType.IsInterface)
        {
            throw new FacadeRegistrationException(
                $"Property {property.Name} in facade {facadeType.Name} must be an interface");
        }

        if (!property.CanRead)
        {
            throw new FacadeRegistrationException(
                $"Property {property.Name} in facade {facadeType.Name} must have a getter");
        }
    }
}