using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.Clinics;

public class GetClinicsRequest : PagedRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
