namespace CentralHealth.Domain.Entities;

public class MedicalService : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "NGN";
    public bool IsActive { get; set; } = true;

    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}
