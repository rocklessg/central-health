using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.Clinics;

public class CreateClinicRequest : AuthenticatedRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}
