using Eventify.Catalog.Application;
using Eventify.Catalog.Domain.Artists;
using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eventify.Catalog.Infrastructure.Configurations;

public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable(CatalogConstants.ArtistsTableName);

        builder.HasKey(a => a.Id);

        builder.Property(prop => prop.Id)
            .HasConversion(id => id.Value, value => ArtistId.Of(value));

        builder.ComplexProperty(prop => prop.Name, propertyBuilder =>
        {
            propertyBuilder.Property(n => n.Value)
                .HasColumnName(nameof(Artist.Name))
                .HasMaxLength(SharedConstants.MaxNameLength)
                .IsRequired();
        });

        builder.Property(prop => prop.Bio)
            .HasMaxLength(SharedConstants.MaxBioLength);

        builder.Property(prop => prop.ImageUrl)
            .HasMaxLength(SharedConstants.MaxImageUrlLength)
            .HasDefaultValue("https://cdn.pixabay.com/photo/2015/10/05/22/37/blank-profile-picture-973460_960_720.png");
    }
}
