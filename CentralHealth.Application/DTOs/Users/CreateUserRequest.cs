using CentralHealth.Application.Common;
using CentralHealth.Domain.Enums;

namespace CentralHealth.Application.DTOs.Users;

public class CreateUserRequest : AuthenticatedRequest
{
    public string NewUsername { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
