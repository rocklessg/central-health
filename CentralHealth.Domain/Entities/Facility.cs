namespace CentralHealth.Domain.Entities;

public class Facility : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Clinic> Clinics { get; set; } = new List<Clinic>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
