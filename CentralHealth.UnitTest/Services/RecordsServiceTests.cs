using CentralHealth.Application.DTOs.Records;
using CentralHealth.Application.Interfaces;
using CentralHealth.Application.Services;
using CentralHealth.Domain.Entities;
using CentralHealth.Domain.Enums;
using CentralHealth.UnitTest.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;

namespace CentralHealth.UnitTest.Services;

public class RecordsServiceTests : ServiceTestBase<RecordsService>
{
    private readonly Mock<IRepository<Appointment>> _appointmentRepositoryMock;
    private readonly Mock<ILogger<RecordsService>> _loggerMock;
    private readonly RecordsService _service;

    public RecordsServiceTests()
    {
        _appointmentRepositoryMock = CreateRepositoryMock<Appointment>();
        _loggerMock = CreateLoggerMock<RecordsService>();

        _service = new RecordsService(
            _appointmentRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetRecordsAsync_WithValidRequest_ReturnsPagedResults()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        patient.Wallet = TestDataFactory.CreateWallet(patient.Id, 5000);
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);
        
        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(9, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(10, 0, 0),
                Status = AppointmentStatus.CheckedIn,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            }
        };

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new RecordsFilterRequest
        {
            FacilityId = facilityId,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetRecordsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        result.Data.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetRecordsAsync_WithClinicFilter_FiltersResults()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var clinicId = TestDataFactory.DefaultClinicId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        patient.Wallet = TestDataFactory.CreateWallet(patient.Id);
        var clinic = TestDataFactory.CreateClinic(id: clinicId, facilityId: facilityId);
        var otherClinic = TestDataFactory.CreateClinic(id: Guid.NewGuid(), facilityId: facilityId);

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinicId,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(9, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = otherClinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(10, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = otherClinic,
                IsDeleted = false
            }
        };

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new RecordsFilterRequest
        {
            FacilityId = facilityId,
            ClinicId = clinicId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetRecordsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().ClinicId.Should().Be(clinicId);
    }

    [Fact]
    public async Task GetRecordsAsync_WithSearchTerm_SearchesPatientNameCodePhone()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient1 = TestDataFactory.CreatePatient(id: Guid.NewGuid(), facilityId: facilityId);
        patient1.FirstName = "John";
        patient1.LastName = "Smith";
        patient1.Wallet = TestDataFactory.CreateWallet(patient1.Id);

        var patient2 = TestDataFactory.CreatePatient(id: Guid.NewGuid(), facilityId: facilityId);
        patient2.FirstName = "Jane";
        patient2.LastName = "Doe";
        patient2.Wallet = TestDataFactory.CreateWallet(patient2.Id);

        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient1.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(9, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient1,
                Clinic = clinic,
                IsDeleted = false
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient2.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(10, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient2,
                Clinic = clinic,
                IsDeleted = false
            }
        };

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new RecordsFilterRequest
        {
            FacilityId = facilityId,
            SearchTerm = "John",
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetRecordsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().PatientName.Should().Contain("John");
    }

    [Fact]
    public async Task GetRecordsAsync_DefaultsToTodayWhenNoDatesProvided()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        patient.Wallet = TestDataFactory.CreateWallet(patient.Id);
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(9, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today.AddDays(-1), // Yesterday
                AppointmentTime = new TimeSpan(10, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            }
        };

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new RecordsFilterRequest
        {
            FacilityId = facilityId,
            // No StartDate or EndDate - should default to today
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetRecordsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1); // Only today's appointment
    }

    [Fact]
    public async Task GetRecordsAsync_IncludesWalletBalanceWithCurrency()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        patient.Wallet = TestDataFactory.CreateWallet(patient.Id, 7500.50m);
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(9, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            }
        };

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new RecordsFilterRequest
        {
            FacilityId = facilityId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetRecordsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        var record = result.Data!.Items.First();
        record.Wallet.Should().NotBeNull();
        record.Wallet.Balance.Should().Be(7500.50m);
        record.Wallet.Currency.Should().Be("NGN");
    }

    [Fact]
    public async Task GetRecordsAsync_SortsByTimeAscendingByDefault()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        patient.Wallet = TestDataFactory.CreateWallet(patient.Id);
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);

        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(14, 0, 0), // 2 PM
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ClinicId = clinic.Id,
                FacilityId = facilityId,
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(9, 0, 0), // 9 AM
                Status = AppointmentStatus.Scheduled,
                Patient = patient,
                Clinic = clinic,
                IsDeleted = false
            }
        };

        var mockQueryable = appointments.AsQueryable().BuildMock();
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new RecordsFilterRequest
        {
            FacilityId = facilityId,
            SortDescending = false, // Ascending
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetRecordsAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        var items = result.Data!.Items.ToList();
        items[0].AppointmentTime.Hour.Should().Be(9);
        items[1].AppointmentTime.Hour.Should().Be(14);
    }
}
