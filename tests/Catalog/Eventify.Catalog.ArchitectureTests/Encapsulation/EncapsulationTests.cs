using Eventify.ArchitectureTests.Commons;

namespace Eventify.Catalog.ArchitectureTests.Encapsulation;

public class EncapsulationTests : BaseTest
{
    [Fact]
    public void Commands_ShouldBeSealed()
    {
        var result = ArchitectureHelper.GetTypesThatShouldBeSealedByEndingNameResult(ApplicationAssembly, "Command");

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void Queries_ShouldBeSealed()
    {
        var result = ArchitectureHelper.GetTypesThatShouldBeSealedByEndingNameResult(ApplicationAssembly, "Query");

        result.ShouldBeSuccessful();
    }
}
