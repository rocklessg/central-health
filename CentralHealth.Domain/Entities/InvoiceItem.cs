namespace CentralHealth.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid? MedicalServiceId { get; set; }
    public MedicalService? MedicalService { get; set; }
}
