namespace CentralHealth.Application.DTOs.Clinics;

public class UpdateClinicRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
