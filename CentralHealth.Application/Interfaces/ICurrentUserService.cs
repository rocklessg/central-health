using CentralHealth.Domain.Enums;

namespace CentralHealth.Application.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid FacilityId { get; }
    string Username { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}
