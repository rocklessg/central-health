using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentralHealth.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PatientCode).HasMaxLength(50).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.MiddleName).HasMaxLength(100);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.Email).HasMaxLength(200);
        builder.Property(p => p.Gender).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(500);

        builder.HasOne(p => p.Facility)
            .WithMany()
            .HasForeignKey(p => p.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Wallet)
            .WithOne(w => w.Patient)
            .HasForeignKey<PatientWallet>(w => w.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.FacilityId, p.PatientCode }).IsUnique();
        builder.HasIndex(p => p.Phone);
        builder.HasIndex(p => new { p.FirstName, p.LastName });

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Ignore(p => p.FullName);
    }
}
