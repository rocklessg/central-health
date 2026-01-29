namespace CentralHealth.Domain.Entities;

public class WalletTransaction : BaseEntity
{
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    public Guid WalletId { get; set; }
    public PatientWallet Wallet { get; set; } = null!;
}
