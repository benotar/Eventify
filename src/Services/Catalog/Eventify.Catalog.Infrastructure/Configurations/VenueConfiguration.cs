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

        builder.Property(prop => prop.Id)
            .HasConversion(id => id.Value, value => VenueId.Create(value));

        builder.ComplexProperty(prop => prop.Name, propertyBuilder =>
        {
            propertyBuilder.Property(n => n.Value)
                .HasColumnName(nameof(Venue.Name))
                .HasMaxLength(SharedConstants.MaxNameLength)
                .IsRequired();
        });

        builder.ComplexProperty(prop => prop.Address, propertyBuilder =>
        {
            propertyBuilder.Property(address => address.Country)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.Country)}")
                .IsRequired();

            propertyBuilder.Property(address => address.City)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.City)}")
                .IsRequired();

            propertyBuilder.Property(address => address.State)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.State)}");

            propertyBuilder.Property(address => address.Street)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.Street)}")
                .IsRequired();

            propertyBuilder.Property(address => address.ZipCode)
                .HasColumnName($"{nameof(Venue.Address)}_{nameof(Venue.Address.ZipCode)}")
                .IsRequired();
        });
    }
}
