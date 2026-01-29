namespace CentralHealth.Application.DTOs.Records;

public class PatientRecordDto
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime AppointmentTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public Guid ClinicId { get; set; }
    public WalletBalanceDto Wallet { get; set; } = new();
}

public class WalletBalanceDto
{
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "NGN";
}
