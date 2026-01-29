using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.MedicalServices;

public class GetMedicalServicesRequest : PagedRequest
{
    public Guid? ClinicId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
