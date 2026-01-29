namespace CentralHealth.Domain.Entities;

public class PatientWallet : BaseEntity
{
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "NGN";

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
