using CentralHealth.Application.Common;
using CentralHealth.Domain.Enums;

namespace CentralHealth.Application.DTOs.Users;

public class GetUsersRequest : PagedRequest
{
    public string? SearchTerm { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
}
