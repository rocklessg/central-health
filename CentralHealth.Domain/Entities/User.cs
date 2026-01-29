using CentralHealth.Domain.Enums;

namespace CentralHealth.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    public ICollection<Payment> ProcessedPayments { get; set; } = new List<Payment>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
