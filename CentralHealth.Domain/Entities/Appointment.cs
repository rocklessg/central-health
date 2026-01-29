using CentralHealth.Domain.Enums;

namespace CentralHealth.Domain.Entities;

public class Appointment : BaseEntity
{
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentType Type { get; set; }
    public string? Notes { get; set; }
    public string? ReasonForVisit { get; set; }

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public Guid FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public DateTime AppointmentDateTime => AppointmentDate.Date.Add(AppointmentTime);
}
