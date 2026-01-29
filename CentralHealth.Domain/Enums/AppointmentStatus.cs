namespace CentralHealth.Domain.Enums;

public enum AppointmentStatus
{
    Scheduled = 1,
    CheckedIn = 2,
    AwaitingPayment = 3,
    AwaitingVitals = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7,
    NoShow = 8
}
