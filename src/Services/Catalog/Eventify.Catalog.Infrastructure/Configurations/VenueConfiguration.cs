using EFCore.ComplexIndexes;
using Eventify.Catalog.Domain.Venues;
using Eventify.Catalog.Domain.Venues.ValueObjects;
using Eventify.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eventify.Catalog.Infrastructure.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable(SharedConstants.VenuesTableName);

        builder.HasKey(venue => venue.Id);

        builder.Ignore(venue => venue.DomainEvents);

        builder.Property(venue => venue.Id)
            .HasConversion(id => id.Value, value => VenueId.Create(value));

        builder.Property(venue => venue.Name)
            .HasConversion(name => name.Value, value => VenueName.Create(value))
            .HasMaxLength(SharedConstants.MaxNameLength)
            .IsRequired();

        builder.HasIndex(venue => venue.Name);

        builder.ComplexProperty(venue => venue.Address, propertyBuilder =>
        {
            propertyBuilder.Property(address => address.Country)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.Country)}")
                .HasMaxLength(SharedConstants.CountryMaxLength)
                .IsRequired();

            propertyBuilder.Property(address => address.City)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.City)}")
                .HasComplexIndex()
                .HasMaxLength(SharedConstants.CityMaxLength)
                .IsRequired();

            propertyBuilder.Property(address => address.State)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.State)}")
                .HasMaxLength(SharedConstants.StateMaxLength);

            propertyBuilder.Property(address => address.Street)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.Street)}")
                .HasMaxLength(SharedConstants.StreetMaxLength)
                .IsRequired();

            propertyBuilder.Property(address => address.ZipCode)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.ZipCode)}")
                .HasComplexIndex()
                .HasMaxLength(SharedConstants.ZipCodeMaxLength)
                .IsRequired();
        });
    }
}
