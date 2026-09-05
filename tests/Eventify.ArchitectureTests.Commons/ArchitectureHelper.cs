using System.Reflection;
using System.Text;
using NetArchTest.Rules;

namespace Eventify.ArchitectureTests.Commons;

public static class ArchitectureHelper
{
    public static TestResult GetNoDependencyOnResult(Assembly sourceAssembly, Assembly forbiddenAssembly)
    {
        if (sourceAssembly == forbiddenAssembly)
        {
            throw new ArgumentException("Assemblies must be different");
        }

        return Types.InAssembly(sourceAssembly)
            .Should()
            .NotHaveDependencyOn(forbiddenAssembly.GetName().Name)
            .GetResult();
    }

    public static TestResult GetHandlerFollowTheNameConventionResult(Assembly assembly, Type interfaceType, string nameEnding)
    {
        return Types.InAssembly(assembly)
            .That()
            .ImplementInterface(interfaceType)
            .Should()
            .HaveNameEndingWith(nameEnding)
            .GetResult();
    }

    public static TestResult GetInterfacesFollowTheNameConventionResult(Assembly[] assemblies)
    {
        return Types.InAssemblies(assemblies)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();
    }

    public static TestResult GetEndpointsFollowTheNameConventionResult(Assembly assembly, Type interfaceType)
    {
        return Types.InAssembly(assembly)
            .That()
            .ImplementInterface(interfaceType)
            .Should()
            .HaveNameEndingWith("Module")
            .And()
            .BeSealed()
            .And()
            .NotBePublic()
            .GetResult();
    }

    public static TestResult GetAssemblyNotInheritTypeResult(Assembly assembly, params Type[] forbiddenTypes)
    {
        if (forbiddenTypes == null || forbiddenTypes.Length == 0)
        {
            throw new ArgumentException($"{nameof(forbiddenTypes)} cannot be null or empty");
        }

        var conditions = Types.InAssembly(assembly).ShouldNot();

        var condition = conditions.Inherit(forbiddenTypes[0]);

        for (var i = 1; i < forbiddenTypes.Length; i++)
        {
            condition = condition.Or().Inherit(forbiddenTypes[i]);
        }

        return condition.GetResult();
    }

    public static TestResult GetAssemblyInNamespaceNoDependencyOnResult(Assembly assembly, string resideInNamespace,
        string dependencyOn)
    {
        return Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceContaining(resideInNamespace)
            .ShouldNot()
            .HaveDependencyOn(dependencyOn)
            .GetResult();
    }

    public static TestResult GetTypesThatShouldBeSealedByEndingNameResult(Assembly assembly, string endingName)
    {
        return Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith(endingName)
            .Should()
            .BeSealed()
            .GetResult();
    }

    public static TestResult GetTypesThatImplementInterfaceHaveNameEndingResult(Assembly assembly,
        string endingName, params Type[] interfaceTypes)
    {
        if (interfaceTypes == null || interfaceTypes.Length == 0)
        {
            throw new ArgumentException($"{nameof(interfaceTypes)} cannot be null or empty");
        }

        var predicates = Types.InAssembly(assembly)
            .That();

        var condition = predicates.ImplementInterface(interfaceTypes[0]);

        for (var i = 1; i < interfaceTypes.Length; i++)
        {
            condition = condition.Or().ImplementInterface(interfaceTypes[i]);
        }

        return condition
            .Should()
            .HaveNameEndingWith(endingName)
            .GetResult();
    }

    public static StringBuilder BuildInvalidItemsMessage(string startText, string butText,
        string[] invalidItems, params string[] insertItems)
    {
        var strBuilder = new StringBuilder();
        strBuilder.Append(startText);

        var added = 0;

        foreach (var item in insertItems)
        {
            strBuilder.Append($"'{item}'");
            added++;

            if (added < insertItems.Length)
            {
                strBuilder.Append(", ");
            }
        }

        strBuilder.Append($". {butText.TrimEnd(":")}:\n{string.Join("\n", invalidItems)}");

        return strBuilder;
    }
}
