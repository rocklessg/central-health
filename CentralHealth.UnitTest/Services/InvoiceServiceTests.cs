using CentralHealth.Application.DTOs.Invoices;
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

public class InvoiceServiceTests : ServiceTestBase<InvoiceService>
{
    private readonly Mock<IRepository<Invoice>> _invoiceRepositoryMock;
    private readonly Mock<IRepository<Patient>> _patientRepositoryMock;
    private readonly Mock<IRepository<Appointment>> _appointmentRepositoryMock;
    private readonly Mock<IRepository<MedicalService>> _medicalServiceRepositoryMock;
    private readonly Mock<ILogger<InvoiceService>> _loggerMock;
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _invoiceRepositoryMock = CreateRepositoryMock<Invoice>();
        _patientRepositoryMock = CreateRepositoryMock<Patient>();
        _appointmentRepositoryMock = CreateRepositoryMock<Appointment>();
        _medicalServiceRepositoryMock = CreateRepositoryMock<MedicalService>();
        _loggerMock = CreateLoggerMock<InvoiceService>();

        _service = new InvoiceService(
            _invoiceRepositoryMock.Object,
            _patientRepositoryMock.Object,
            _appointmentRepositoryMock.Object,
            _medicalServiceRepositoryMock.Object,
            UnitOfWorkMock.Object,
            ValidationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithValidRequest_CreatesInvoice()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        _patientRepositoryMock.Setup(x => x.Query()).Returns(new List<Patient> { patient }.AsQueryable().BuildMock());
        _appointmentRepositoryMock.Setup(x => x.Query()).Returns(new List<Appointment>().AsQueryable().BuildMock());
        var request = new CreateInvoiceRequest
        {
            FacilityId = facilityId,
            PatientId = patient.Id,
            DiscountPercentage = 0,
            Items = new List<CreateInvoiceItemRequest>
            {
                new CreateInvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 5000, DiscountAmount = 0 }
            },
            Username = "frontdesk"
        };
        // Act
        var result = await _service.CreateInvoiceAsync(request);
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PatientId.Should().Be(patient.Id);
        result.Data.Items.Should().HaveCount(1);
        result.Data.TotalAmount.Should().Be(5000);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithDiscount_CalculatesTotalCorrectly()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        _patientRepositoryMock.Setup(x => x.Query()).Returns(new List<Patient> { patient }.AsQueryable().BuildMock());
        var request = new CreateInvoiceRequest
        {
            FacilityId = facilityId,
            PatientId = patient.Id,
            DiscountPercentage = 10, // 10% discount
            Items = new List<CreateInvoiceItemRequest>
            {
                new CreateInvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 1000, DiscountAmount = 0 }
            },
            Username = "frontdesk"
        };
        // Act
        var result = await _service.CreateInvoiceAsync(request);
        // Assert
        result.Success.Should().BeTrue();
        result.Data!.SubTotal.Should().Be(1000);
        result.Data.DiscountAmount.Should().Be(100); // 10% of 1000
        result.Data.TotalAmount.Should().Be(900);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithInvalidPatient_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        _patientRepositoryMock.Setup(x => x.Query()).Returns(new List<Patient>().AsQueryable().BuildMock());
        var request = new CreateInvoiceRequest
        {
            FacilityId = facilityId,
            PatientId = Guid.NewGuid(),
            DiscountPercentage = 0,
            Items = new List<CreateInvoiceItemRequest>
            {
                new CreateInvoiceItemRequest { Description = "Consultation", Quantity = 1, UnitPrice = 5000, DiscountAmount = 0 }
            },
            Username = "frontdesk"
        };
        // Act
        var result = await _service.CreateInvoiceAsync(request);
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Patient not found");
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_WithValidId_ReturnsInvoice()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId);
        invoice.Patient = patient;
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(new List<Invoice> { invoice }.AsQueryable().BuildMock());
        // Act
        var result = await _service.GetInvoiceByIdAsync(invoice.Id, facilityId);
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(invoice.Id);
    }
}
