using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentralHealth.Infrastructure.Data.Configurations;

public class MedicalServiceConfiguration : IEntityTypeConfiguration<MedicalService>
{
    public void Configure(EntityTypeBuilder<MedicalService> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.UnitPrice).HasPrecision(18, 2);
        builder.Property(s => s.Currency).HasMaxLength(10).IsRequired();

        builder.HasOne(s => s.Clinic)
            .WithMany(c => c.MedicalServices)
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ClinicId, s.Code }).IsUnique();

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
