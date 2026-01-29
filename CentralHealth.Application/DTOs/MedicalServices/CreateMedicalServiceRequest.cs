namespace CentralHealth.Application.DTOs.MedicalServices;

public class CreateMedicalServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid ClinicId { get; set; }
}
