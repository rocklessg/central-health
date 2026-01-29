using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.Clinics;

public class UpdateClinicRequest : AuthenticatedRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
