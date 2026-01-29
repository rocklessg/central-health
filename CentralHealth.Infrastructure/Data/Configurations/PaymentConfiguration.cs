using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CentralHealth.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PaymentReference).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(10).IsRequired();
        builder.Property(p => p.TransactionId).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ProcessedBy)
            .WithMany(u => u.ProcessedPayments)
            .HasForeignKey(p => p.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PaymentReference).IsUnique();

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
