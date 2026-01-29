using CentralHealth.Application.Common;

namespace CentralHealth.Application.DTOs.Records;

public class RecordsFilterRequest : PagedRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ClinicId { get; set; }
    public string? SearchTerm { get; set; }
    public bool SortDescending { get; set; } = false;
}
