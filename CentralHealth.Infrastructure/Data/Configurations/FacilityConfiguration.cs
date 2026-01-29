using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentralHealth.Infrastructure.Data.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Code).HasMaxLength(50).IsRequired();
        builder.Property(f => f.Address).HasMaxLength(500);
        builder.Property(f => f.Phone).HasMaxLength(50);
        builder.Property(f => f.Email).HasMaxLength(200);

        builder.HasIndex(f => f.Code).IsUnique();

        builder.HasQueryFilter(f => !f.IsDeleted);
    }
}
