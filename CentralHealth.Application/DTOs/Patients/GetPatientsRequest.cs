using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.Patients;

public class GetPatientsRequest : PagedRequest
{
    public string? SearchTerm { get; set; }
}
