using System.Reflection;

namespace Core.Infrastructure.Configurations;

internal static class InstanceFactory
{
    internal static IEnumerable<T> CreateFromAssemblies<T>(params Assembly[] assemblies) =>
        assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(IsAssignableToType<T>)
            .Select(type => (T)Activator.CreateInstance(type)!)
            .Cast<T>();

    private static bool IsAssignableToType<T>(TypeInfo typeInfo) =>
        typeof(T).IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
}