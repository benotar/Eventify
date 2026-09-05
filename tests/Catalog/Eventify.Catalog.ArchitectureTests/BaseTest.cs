using System.Reflection;
using Eventify.Catalog.Api;
using Eventify.Catalog.Application;
using Eventify.Catalog.Domain;
using Eventify.Catalog.Infrastructure;

namespace Eventify.Catalog.ArchitectureTests;

public abstract class BaseTest
{
    static protected readonly Assembly DomainAssembly = typeof(ICatalogDomainMarker).Assembly;
    static protected readonly Assembly ApplicationAssembly = typeof(ICatalogApplicationMarker).Assembly;
    static protected readonly Assembly InfrastructureAssembly = typeof(ICatalogInfrastructureMarker).Assembly;
    static protected readonly Assembly PresentationAssembly = typeof(ICatalogPresentationMarker).Assembly;
}
