namespace CentralHealth.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 1,
    Pending = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5,
    Refunded = 6
}
