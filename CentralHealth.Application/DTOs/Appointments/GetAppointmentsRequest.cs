using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.Appointments;

public class GetAppointmentsRequest : PagedRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ClinicId { get; set; }
}
