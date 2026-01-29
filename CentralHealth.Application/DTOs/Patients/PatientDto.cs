namespace CentralHealth.Application.DTOs.Patients;

public class PatientDto
{
    public Guid Id { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public decimal WalletBalance { get; set; }
    public string WalletCurrency { get; set; } = "NGN";
    public DateTime CreatedAt { get; set; }
}
