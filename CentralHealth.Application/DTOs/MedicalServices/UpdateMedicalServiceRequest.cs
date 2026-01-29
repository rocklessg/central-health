namespace CentralHealth.Application.DTOs.MedicalServices;

public class UpdateMedicalServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
}
