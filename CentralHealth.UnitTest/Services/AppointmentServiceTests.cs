using CentralHealth.Application.DTOs.Appointments;
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

public class AppointmentServiceTests : ServiceTestBase<AppointmentService>
{
    private readonly Mock<IRepository<Appointment>> _appointmentRepositoryMock;
    private readonly Mock<IRepository<Patient>> _patientRepositoryMock;
    private readonly Mock<IRepository<Clinic>> _clinicRepositoryMock;
    private readonly Mock<ILogger<AppointmentService>> _loggerMock;
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        _appointmentRepositoryMock = CreateRepositoryMock<Appointment>();
        _patientRepositoryMock = CreateRepositoryMock<Patient>();
        _clinicRepositoryMock = CreateRepositoryMock<Clinic>();
        _loggerMock = CreateLoggerMock<AppointmentService>();

        _service = new AppointmentService(
            _appointmentRepositoryMock.Object,
            _patientRepositoryMock.Object,
            _clinicRepositoryMock.Object,
            UnitOfWorkMock.Object,
            ValidationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAppointmentAsync_WithValidRequest_CreatesAppointment()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);
        var patients = new List<Patient> { patient }.AsQueryable().BuildMock();
        var clinics = new List<Clinic> { clinic }.AsQueryable().BuildMock();
        _patientRepositoryMock.Setup(x => x.Query()).Returns(patients);
        _clinicRepositoryMock.Setup(x => x.Query()).Returns(clinics);
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(new List<Appointment>().AsQueryable().BuildMock());
        var request = new CreateAppointmentRequest
        {
            FacilityId = facilityId,
            PatientId = patient.Id,
            ClinicId = clinic.Id,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(9, 0, 0),
            Type = AppointmentType.Consultation,
            Username = "frontdesk"
        };
        // Act
        var result = await _service.CreateAppointmentAsync(request);
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PatientId.Should().Be(patient.Id);
        result.Data.ClinicId.Should().Be(clinic.Id);
    }

    [Fact]
    public async Task CreateAppointmentAsync_WithDuplicateAppointment_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);
        var existing = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            ClinicId = clinic.Id,
            FacilityId = facilityId,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(9, 0, 0),
            Status = AppointmentStatus.Scheduled,
            IsDeleted = false
        };
        _patientRepositoryMock.Setup(x => x.Query()).Returns(new List<Patient> { patient }.AsQueryable().BuildMock());
        _clinicRepositoryMock.Setup(x => x.Query()).Returns(new List<Clinic> { clinic }.AsQueryable().BuildMock());
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(new List<Appointment> { existing }.AsQueryable().BuildMock());
        var request = new CreateAppointmentRequest
        {
            FacilityId = facilityId,
            PatientId = patient.Id,
            ClinicId = clinic.Id,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(9, 0, 0),
            Type = AppointmentType.Consultation,
            Username = "frontdesk"
        };
        // Act
        var result = await _service.CreateAppointmentAsync(request);
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already has an appointment");
    }

    [Fact]
    public async Task CreateAppointmentAsync_WithInvalidPatient_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);
        _patientRepositoryMock.Setup(x => x.Query()).Returns(new List<Patient>().AsQueryable().BuildMock());
        _clinicRepositoryMock.Setup(x => x.Query()).Returns(new List<Clinic> { clinic }.AsQueryable().BuildMock());
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(new List<Appointment>().AsQueryable().BuildMock());
        var request = new CreateAppointmentRequest
        {
            FacilityId = facilityId,
            PatientId = Guid.NewGuid(),
            ClinicId = clinic.Id,
            AppointmentDate = DateTime.Today,
            AppointmentTime = new TimeSpan(9, 0, 0),
            Type = AppointmentType.Consultation,
            Username = "frontdesk"
        };
        // Act
        var result = await _service.CreateAppointmentAsync(request);
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Patient not found");
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_WithValidId_ReturnsAppointment()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var clinic = TestDataFactory.CreateClinic(facilityId: facilityId);
        var appointment = TestDataFactory.CreateAppointment(facilityId: facilityId);
        appointment.Patient = patient;
        appointment.Clinic = clinic;
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(new List<Appointment> { appointment }.AsQueryable().BuildMock());
        // Act
        var result = await _service.GetAppointmentByIdAsync(appointment.Id, facilityId);
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(appointment.Id);
    }
}
