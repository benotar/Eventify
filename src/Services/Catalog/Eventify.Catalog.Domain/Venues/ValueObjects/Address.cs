using Eventify.SharedKernel.Domain.Exceptions;
using Eventify.SharedKernel.Extensions;

namespace Eventify.Catalog.Domain.Venues.ValueObjects;

public record Address
{
    public string Country { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string Street { get; private set; }
    public string ZipCode { get; private set; }

    private Address(string country, string city, string state, string street, string zipCode)
    {
        Country = country;
        City = city;
        State = state;
        Street = street;
        ZipCode = zipCode;
    }

    public static Address Create(string country, string city, string state, string street, string zipCode)
    {
        DomainException.ThrowIfNullOrEmpty(country, "Country cannot be empty");
        DomainException.ThrowIfNullOrEmpty(city, "City cannot be empty");
        //DomainException.ThrowIfNullOrEmpty(state, "State cannot be empty");
        DomainException.ThrowIfNullOrEmpty(street, "Street cannot be empty");
        DomainException.ThrowIfNullOrEmpty(zipCode, "ZipCode cannot be empty");

        return new Address(country, city, state, street, zipCode);
    }
}
