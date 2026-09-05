using Eventify.ArchitectureTests.Commons;

namespace Eventify.Catalog.ArchitectureTests.Conventions;

public class EndpointModulesNamingConventionsTests : BaseTest
{
    [Fact]
    public void ArtistModuleNestedModels_Should_FollowTheNameConvention()
    {
        var invalidNames = PresentationAssembly.GeInvalidEndpointModuleNestedTypes("ArtistModule")
            .ToInvalidNames();

        invalidNames.ShouldBeEmptyAndEnd("Request", "Response");
    }
}
