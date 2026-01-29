using CentralHealth.Domain.Entities;
using CentralHealth.Domain.Enums;

namespace CentralHealth.UnitTest.Common;

public static class TestDataFactory
{
    public static Guid DefaultFacilityId => Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static Guid DefaultPatientId => Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static Guid DefaultClinicId => Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static Guid DefaultAppointmentId => Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static Guid DefaultInvoiceId => Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static string DefaultUsername => "testuser";

    public static Facility CreateFacility(Guid? id = null)
    {
        return new Facility
        {
            Id = id ?? DefaultFacilityId,
            Name = "Test Hospital",
            Code = "TH-001",
            Address = "123 Test Street",
            Phone = "+234-800-000-0000",
            Email = "test@hospital.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }

    public static Clinic CreateClinic(Guid? id = null, Guid? facilityId = null)
    {
        return new Clinic
        {
            Id = id ?? DefaultClinicId,
            FacilityId = facilityId ?? DefaultFacilityId,
            Name = "General Clinic",
            Code = "GC-001",
            Description = "General medicine clinic",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }

    public static Patient CreatePatient(Guid? id = null, Guid? facilityId = null, PatientWallet? wallet = null)
    {
        var patient = new Patient
        {
            Id = id ?? DefaultPatientId,
            FacilityId = facilityId ?? DefaultFacilityId,
            PatientCode = "PAT-20240115-ABC123",
            FirstName = "John",
            LastName = "Doe",
            Phone = "+234-800-111-1111",
            Email = "john.doe@email.com",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        if (wallet != null)
        {
            patient.Wallet = wallet;
        }

        return patient;
    }

    public static PatientWallet CreateWallet(Guid patientId, decimal balance = 10000)
    {
        return new PatientWallet
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Balance = balance,
            Currency = "NGN",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }

    public static Appointment CreateAppointment(
        Guid? id = null,
        Guid? patientId = null,
        Guid? clinicId = null,
        Guid? facilityId = null,
        AppointmentStatus status = AppointmentStatus.Scheduled)
    {
        var patient = CreatePatient(patientId, facilityId);
        var clinic = CreateClinic(clinicId, facilityId);

        return new Appointment
        {
            Id = id ?? DefaultAppointmentId,
            PatientId = patientId ?? DefaultPatientId,
            ClinicId = clinicId ?? DefaultClinicId,
            FacilityId = facilityId ?? DefaultFacilityId,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(9, 0, 0),
            Type = AppointmentType.Consultation,
            Status = status,
            ReasonForVisit = "General checkup",
            Patient = patient,
            Clinic = clinic,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }

    public static Invoice CreateInvoice(
        Guid? id = null,
        Guid? patientId = null,
        Guid? facilityId = null,
        Guid? appointmentId = null,
        InvoiceStatus status = InvoiceStatus.Pending,
        decimal totalAmount = 5000,
        decimal paidAmount = 0)
    {
        var patient = CreatePatient(patientId, facilityId);

        return new Invoice
        {
            Id = id ?? DefaultInvoiceId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-ABCD1234",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            PatientId = patientId ?? DefaultPatientId,
            AppointmentId = appointmentId,
            FacilityId = facilityId ?? DefaultFacilityId,
            SubTotal = totalAmount,
            DiscountPercentage = 0,
            DiscountAmount = 0,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            Currency = "NGN",
            Status = status,
            Patient = patient,
            Items = new List<InvoiceItem>(),
            Payments = new List<Payment>(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }

    public static Payment CreatePayment(
        Guid? id = null,
        Guid? invoiceId = null,
        decimal amount = 5000,
        PaymentMethod method = PaymentMethod.Cash)
    {
        return new Payment
        {
            Id = id ?? Guid.NewGuid(),
            PaymentReference = $"PAY-{DateTime.UtcNow:yyyyMMdd}-ABCD1234",
            InvoiceId = invoiceId ?? DefaultInvoiceId,
            Amount = amount,
            Currency = "NGN",
            Method = method,
            Status = PaymentStatus.Completed,
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }
}
