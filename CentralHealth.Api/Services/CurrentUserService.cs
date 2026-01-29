using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Enums;

namespace CentralHealth.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }

    public Guid FacilityId
    {
        get
        {
            var facilityIdClaim = _httpContextAccessor.HttpContext?.Request.Headers["X-Facility-Id"].FirstOrDefault();
            return Guid.TryParse(facilityIdClaim, out var facilityId) ? facilityId : Guid.Empty;
        }
    }

    public string Username
    {
        get
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-Username"].FirstOrDefault() ?? "system";
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Role"].FirstOrDefault();
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.FrontDesk;
        }
    }

    public bool IsAuthenticated => UserId != Guid.Empty && FacilityId != Guid.Empty;
}
