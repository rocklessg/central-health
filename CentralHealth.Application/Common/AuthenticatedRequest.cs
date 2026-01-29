namespace CentralHealth.Application.Common;

public abstract class AuthenticatedRequest
{
    public Guid FacilityId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = "system";
}
