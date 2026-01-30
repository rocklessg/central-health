using System.ComponentModel.DataAnnotations;

namespace CentralHealth.Application.Common;

public abstract class AuthenticatedRequest
{
    [Required]
    public Guid FacilityId { get; set; }
    public string Username { get; set; } = "system";
}
