namespace CentralHealth.Application.DTOs.Facilities;

public class CreateFacilityResponse
{
    public FacilityDto Facility { get; set; } = new();
    public AdminUserResponse AdminUser { get; set; } = new();
}

public class AdminUserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
