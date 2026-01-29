using CentralHealth.Domain.Enums;

namespace CentralHealth.Application.DTOs.Payments;

public class ProcessPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}
