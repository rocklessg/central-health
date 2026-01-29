using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentralHealth.Infrastructure.Data.Configurations;

public class PatientWalletConfiguration : IEntityTypeConfiguration<PatientWallet>
{
    public void Configure(EntityTypeBuilder<PatientWallet> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Balance).HasPrecision(18, 2);
        builder.Property(w => w.Currency).HasMaxLength(10).IsRequired();

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
