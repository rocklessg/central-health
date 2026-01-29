namespace CentralHealth.Application.DTOs.MedicalServices;

public class MedicalServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "NGN";
    public bool IsActive { get; set; }
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
