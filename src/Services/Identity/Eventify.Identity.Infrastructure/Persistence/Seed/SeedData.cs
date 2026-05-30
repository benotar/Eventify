using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Eventify.Identity.Infrastructure.Options;

namespace Eventify.Identity.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
        return
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        ];
    }

    public static IEnumerable<ApiScope> GetApiScopes()
    {
        return
        [
            new ApiScope("catalog.read"),
            new ApiScope("catalog.write")
        ];
    }

    public static IEnumerable<ApiResource> GetApiResources()
    {
        return
        [
            new ApiResource("catalog-api", "Catalog API") { Scopes = { "catalog.read", "catalog.write" } }
        ];
    }

    public static IEnumerable<Client> GetClients(ServicesOptions options)
    {
        return
        [
            new Client
            {
                ClientId = "scalar-catalog",
                ClientName = "Scalar (Catalog API)",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris = { $"{options.Catalog}/scalar/oauth2-redirect" },
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "catalog.read",
                    "catalog.write"
                }
            },
            new Client
            {
                ClientId = "eventify-spa",
                ClientName = "Eventify SPA",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris = { $"{options.Spa}/callback" },
                PostLogoutRedirectUris = { $"{options.Spa}" },
                AllowedCorsOrigins = { $"{options.Spa}" },
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "catalog.read",
                    "catalog.write"
                },
                AllowOfflineAccess = true
            }
        ];
    }
}
