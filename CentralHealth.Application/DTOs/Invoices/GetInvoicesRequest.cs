using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.Invoices;

public class GetInvoicesRequest : PagedRequest
{
    public Guid? PatientId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
