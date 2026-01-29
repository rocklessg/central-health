using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentralHealth.Infrastructure.Data.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasOne(c => c.Facility)
            .WithMany(f => f.Clinics)
            .HasForeignKey(c => c.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.FacilityId, c.Code }).IsUnique();

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
