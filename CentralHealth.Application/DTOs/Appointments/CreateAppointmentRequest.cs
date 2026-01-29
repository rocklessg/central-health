using CentralHealth.Application.Common;
using CentralHealth.Domain.Enums;

namespace CentralHealth.Application.DTOs.Appointments;

public class CreateAppointmentRequest : AuthenticatedRequest
{
    public Guid PatientId { get; set; }
    public Guid ClinicId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }
    public AppointmentType Type { get; set; }
    public string? ReasonForVisit { get; set; }
    public string? Notes { get; set; }
}
