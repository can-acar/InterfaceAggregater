namespace InterfaceAggregate;

public sealed class DefaultImplementationTypeResolver : IImplementationTypeResolver
{
    private readonly ConcurrentDictionary<Type, Type> _implementationCache;
    private readonly Lazy<IEnumerable<Assembly>> _assemblies;

    public DefaultImplementationTypeResolver()
    {
        _implementationCache = new ConcurrentDictionary<Type, Type>();
        _assemblies = new Lazy<IEnumerable<Assembly>>(() => AppDomain.CurrentDomain.GetAssemblies());
    }

    public Type ResolveImplementationType(Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(interfaceType);
        return _implementationCache.GetOrAdd(interfaceType, ResolveImplementationTypeInternal);
    }

    private Type ResolveImplementationTypeInternal(Type interfaceType)
    {
        var implementations = _assemblies.Value
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes()
                        .Where(t => IsValidImplementation(t, interfaceType));
                }
                catch (Exception ex)
                {
                    return [];
                }
            })
            .ToList();

        return SelectBestImplementation(interfaceType, implementations);
    }

    private static bool IsValidImplementation(Type type, Type interfaceType) =>
        type is { IsClass: true, IsAbstract: false } && interfaceType.IsAssignableFrom(type);

    private Type SelectBestImplementation(Type interfaceType, IReadOnlyCollection<Type> implementations)
    {
        if (!implementations.Any())
        {
            throw new FacadeRegistrationException($"No implementation found for interface {interfaceType.Name}");
        }

        if (implementations.Count == 1)
        {
            return implementations.First();
        }

        var bestMatch = FindBestMatch(interfaceType.Name, implementations);
        if (bestMatch != null)
        {
            return bestMatch;
        }

        throw new FacadeRegistrationException(
            $"Multiple implementations found for interface {interfaceType.Name}. " +
            $"Found types: {string.Join(", ", implementations.Select(t => t.Name))}");
    }

    private static Type FindBestMatch(string interfaceName, IEnumerable<Type> implementations)
    {
        var searchName = interfaceName.StartsWith("I") ? interfaceName[1..] : interfaceName;

        var conventionNames = new[]
        {
            searchName,
            $"{searchName}Implementation",
            $"{searchName}Repository"
        };

        return implementations.FirstOrDefault(t =>
            conventionNames.Any(name =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }
}