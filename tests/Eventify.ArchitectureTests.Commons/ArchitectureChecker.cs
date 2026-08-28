using System.Reflection;
using NetArchTest.Rules;

namespace Eventify.ArchitectureTests.Commons;

public static class ArchitectureChecker
{
    public static TestResult GetNoDependencyOnResult(Assembly source, Assembly forbidden)
    {
        if (source == forbidden)
        {
            throw new ArgumentException("Assemblies must be different");
        }

        return Types.InAssembly(source)
            .Should()
            .NotHaveDependencyOn(forbidden.GetName().Name)
            .GetResult();
    }

    public static TestResult GetHandlerImplementInterfaceResult(Assembly source, Type interfaceType, string nameEnding)
    {
        return Types.InAssembly(source)
            .That()
            .ImplementInterface(interfaceType)
            .Should()
            .NotBePublic()
            .And()
            .HaveNameEndingWith(nameEnding)
            .And()
            .BeSealed()
            .GetResult();
    }
}
