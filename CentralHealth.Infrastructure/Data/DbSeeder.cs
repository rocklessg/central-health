using CentralHealth.Domain.Entities;
using CentralHealth.Domain.Enums;
using CentralHealth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentralHealth.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Facilities.AnyAsync())
            return;

        var facilityId = Guid.NewGuid();
        var facility = new Facility
        {
            Id = facilityId,
            Name = "Central Health Main Hospital",
            Code = "CH-001",
            Address = "123 Healthcare Avenue, Medical City",
            Phone = "+234-800-000-0001",
            Email = "info@centralhealth.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        await context.Facilities.AddAsync(facility);

        var clinic1Id = Guid.NewGuid();
        var clinic2Id = Guid.NewGuid();
        var clinics = new List<Clinic>
        {
            new()
            {
                Id = clinic1Id,
                FacilityId = facilityId,
                Name = "General Medicine",
                Code = "GM-001",
                Description = "General medical consultations and treatments",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            },
            new()
            {
                Id = clinic2Id,
                FacilityId = facilityId,
                Name = "Pediatrics",
                Code = "PED-001",
                Description = "Child healthcare services",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            }
        };

        await context.Clinics.AddRangeAsync(clinics);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FacilityId = facilityId,
            Username = "frontdesk1",
            Email = "frontdesk1@centralhealth.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "hashed_password_placeholder",
            Role = UserRole.FrontDesk,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        await context.Users.AddAsync(user);

        var patient1Id = Guid.NewGuid();
        var patient2Id = Guid.NewGuid();
        var patients = new List<Patient>
        {
            new()
            {
                Id = patient1Id,
                FacilityId = facilityId,
                PatientCode = "PAT-001",
                FirstName = "Jane",
                LastName = "Smith",
                Phone = "+234-801-234-5678",
                Email = "jane.smith@email.com",
                DateOfBirth = new DateTime(1990, 5, 15),
                Gender = "Female",
                Address = "456 Patient Street",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            },
            new()
            {
                Id = patient2Id,
                FacilityId = facilityId,
                PatientCode = "PAT-002",
                FirstName = "Michael",
                LastName = "Johnson",
                Phone = "+234-802-345-6789",
                Email = "michael.j@email.com",
                DateOfBirth = new DateTime(1985, 8, 20),
                Gender = "Male",
                Address = "789 Health Lane",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            }
        };

        await context.Patients.AddRangeAsync(patients);

        var wallets = new List<PatientWallet>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patient1Id,
                Balance = 50000.00m,
                Currency = "NGN",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patient2Id,
                Balance = 25000.00m,
                Currency = "NGN",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            }
        };

        await context.PatientWallets.AddRangeAsync(wallets);

        var services = new List<MedicalService>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic1Id,
                Name = "General Consultation",
                Code = "SVC-001",
                Description = "Standard doctor consultation",
                UnitPrice = 5000.00m,
                Currency = "NGN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic1Id,
                Name = "Blood Test",
                Code = "SVC-002",
                Description = "Complete blood count test",
                UnitPrice = 8000.00m,
                Currency = "NGN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic2Id,
                Name = "Pediatric Consultation",
                Code = "SVC-003",
                Description = "Child health consultation",
                UnitPrice = 6000.00m,
                Currency = "NGN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            }
        };

        await context.MedicalServices.AddRangeAsync(services);

        var appointments = new List<Appointment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FacilityId = facilityId,
                PatientId = patient1Id,
                ClinicId = clinic1Id,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(9, 0, 0),
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                ReasonForVisit = "Regular checkup",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            },
            new()
            {
                Id = Guid.NewGuid(),
                FacilityId = facilityId,
                PatientId = patient2Id,
                ClinicId = clinic1Id,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(10, 30, 0),
                Type = AppointmentType.FollowUp,
                Status = AppointmentStatus.CheckedIn,
                ReasonForVisit = "Follow-up visit",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            }
        };

        await context.Appointments.AddRangeAsync(appointments);

        await context.SaveChangesAsync();
    }
}
