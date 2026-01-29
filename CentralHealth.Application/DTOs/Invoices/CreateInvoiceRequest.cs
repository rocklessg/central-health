namespace CentralHealth.Application.DTOs.Invoices;

public class CreateInvoiceRequest
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}

public class CreateInvoiceItemRequest
{
    public Guid? MedicalServiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}
