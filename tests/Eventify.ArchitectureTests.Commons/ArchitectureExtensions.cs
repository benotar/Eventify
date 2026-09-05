using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace Eventify.ArchitectureTests.Commons;

public static class ArchitectureExtensions
{
    extension(string[] invalidItems)
    {
        public void ShouldBeEmptyAndEnd(params string[] endItems)
        {
            const string startText = "All nested types must ends on ";
            const string butText = "But found";

            var strBuilder = ArchitectureHelper.BuildInvalidItemsMessage(startText, butText, invalidItems, endItems);

            invalidItems.ShouldBeEmpty(strBuilder.ToString());
        }

        public void ShouldBeEmptyAndImplement(string unitName, params Type[] implementItems)
        {
            var startText = $"All {unitName} must implement ";
            const string butText = "But found not implemented";

            var strBuilder = ArchitectureHelper.BuildInvalidItemsMessage(startText, butText, invalidItems,
                implementItems.Select(i => i.Name).ToArray());

            invalidItems.ShouldBeEmpty(strBuilder.ToString());
        }
    }

    public static void ShouldBeSuccessful(this TestResult result)
    {
        result.IsSuccessful.ShouldBeTrue(
            $"Failing types: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    public static string[] ToInvalidNames(this IEnumerable<Type> types)
    {
        return types.Select(type => type.DeclaringType is not null ? $"{type.DeclaringType.Name}.{type.Name}" : type.Name)
            .ToArray();
    }

    public static string[] ToInvalidNames(this IEnumerable<MethodInfo> methods)
    {
        return methods.Select(type => $"{type.DeclaringType?.Name}.{type.Name}").ToArray();
    }

    public static bool IsAsyncMethodWithoutAsyncSuffix(this MethodInfo methodInfo)
    {
        return (typeof(Task).IsAssignableFrom(methodInfo.ReturnType)
                || typeof(ValueTask).IsAssignableFrom(methodInfo.ReturnType)
                || methodInfo.ReturnType.IsGenericType
                && (methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                    || methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
               && !methodInfo.Name.EndsWith("Async");
    }

    public static IEnumerable<Type> GeInvalidEndpointModuleNestedTypes(this Assembly assembly, string moduleName)
    {
        var types = assembly
            .GetTypes()
            .Where(type => type.IsClass && type.Name.Equals(moduleName));

        var nestedTypes = types
            .SelectMany(type => type.GetNestedTypes())
            .ToList();

        return nestedTypes
            .Where(type => !type.Name.EndsWith("Request") && !type.Name.EndsWith("Response"));
    }

    public static IEnumerable<MethodInfo> GetMethodsByPredicate(this IEnumerable<Assembly> assemblies,
        Func<MethodInfo, bool> predicate)
    {
        return Types.InAssemblies(assemblies)
            .That()
            .AreClasses()
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(predicate);
    }

    public static IEnumerable<Type> GetNotImplementedInterface(this Assembly assembly, string endingName,
        params Type[] interfaceTypes)
    {
        if (interfaceTypes == null || interfaceTypes.Length == 0)
        {
            throw new ArgumentException($"{nameof(interfaceTypes)} cannot be null or empty");
        }

        var handlerTypes = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith(endingName)
            .GetTypes();

        return handlerTypes.Where(type => !type.ImplementsAnyInterface(interfaceTypes));
    }

    public static bool ImplementsAnyInterface(this Type type, Type[] interfaceTypes)
    {
        return type.GetInterfaces()
            .Any(i => interfaceTypes.Contains(i.IsGenericType ? i.GetGenericTypeDefinition() : i));
    }
}
