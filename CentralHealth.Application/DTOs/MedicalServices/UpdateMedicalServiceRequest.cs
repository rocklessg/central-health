using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.MedicalServices;

public class UpdateMedicalServiceRequest : AuthenticatedRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
}
