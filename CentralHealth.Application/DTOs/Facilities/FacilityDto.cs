namespace CentralHealth.Application.DTOs.Facilities;

public class FacilityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public int ClinicsCount { get; set; }
    public int UsersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
