using CentralHealth.Application.DTOs.Payments;
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

public class PaymentServiceTests : ServiceTestBase<PaymentService>
{
    private readonly Mock<IRepository<Payment>> _paymentRepositoryMock;
    private readonly Mock<IRepository<Invoice>> _invoiceRepositoryMock;
    private readonly Mock<IRepository<Appointment>> _appointmentRepositoryMock;
    private readonly Mock<IRepository<PatientWallet>> _walletRepositoryMock;
    private readonly Mock<IRepository<WalletTransaction>> _walletTransactionRepositoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = CreateRepositoryMock<Payment>();
        _invoiceRepositoryMock = CreateRepositoryMock<Invoice>();
        _appointmentRepositoryMock = CreateRepositoryMock<Appointment>();
        _walletRepositoryMock = CreateRepositoryMock<PatientWallet>();
        _walletTransactionRepositoryMock = CreateRepositoryMock<WalletTransaction>();
        _loggerMock = CreateLoggerMock<PaymentService>();

        _service = new PaymentService(
            _paymentRepositoryMock.Object,
            _invoiceRepositoryMock.Object,
            _appointmentRepositoryMock.Object,
            _walletRepositoryMock.Object,
            _walletTransactionRepositoryMock.Object,
            UnitOfWorkMock.Object,
            ValidationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequest_CreatesPayment()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId, totalAmount: 5000);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 5000,
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Amount.Should().Be(5000);
        result.Data.Method.Should().Be("Cash");
        result.Data.Status.Should().Be("Completed");

        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithFullPayment_UpdatesInvoiceStatusToPaid()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId, totalAmount: 5000, paidAmount: 0);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 5000, // Full payment
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAmount.Should().Be(5000);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithPartialPayment_UpdatesInvoiceStatusToPartiallyPaid()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId, totalAmount: 5000, paidAmount: 0);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 2000, // Partial payment
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.PaidAmount.Should().Be(2000);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenInvoiceNotFound_ReturnsFailure()
    {
        // Arrange
        var invoices = new List<Invoice>();
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = TestDataFactory.DefaultFacilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = Guid.NewGuid(),
            Amount = 5000,
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invoice not found");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenInvoiceAlreadyPaid_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(
            facilityId: facilityId,
            totalAmount: 5000,
            paidAmount: 5000,
            status: InvoiceStatus.Paid);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 1000,
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invoice is already paid");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenAmountExceedsOutstanding_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId, totalAmount: 5000, paidAmount: 4000);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 2000, // Outstanding is only 1000
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.First().Should().Contain("exceeds outstanding amount");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithWalletPayment_DeductsFromWallet()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var wallet = TestDataFactory.CreateWallet(patient.Id, 10000);
        patient.Wallet = wallet;
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId, totalAmount: 5000);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 5000,
            Method = PaymentMethod.Wallet
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        wallet.Balance.Should().Be(5000); // 10000 - 5000
        _walletRepositoryMock.Verify(x => x.Update(wallet), Times.Once);
        _walletTransactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<WalletTransaction>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithInsufficientWalletBalance_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var wallet = TestDataFactory.CreateWallet(patient.Id, 1000); // Only 1000 balance
        patient.Wallet = wallet;
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId, totalAmount: 5000);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 5000,
            Method = PaymentMethod.Wallet
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Insufficient wallet balance");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenFullPaymentWithAppointment_MovesToAwaitingVitals()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var appointment = TestDataFactory.CreateAppointment(
            facilityId: facilityId,
            status: AppointmentStatus.AwaitingPayment);
        var invoice = TestDataFactory.CreateInvoice(
            facilityId: facilityId,
            appointmentId: appointment.Id,
            totalAmount: 5000);
        invoice.Patient = patient;
        invoice.Appointment = appointment;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        var request = new ProcessPaymentRequest
        {
            FacilityId = facilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = invoice.Id,
            Amount = 5000, // Full payment
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.AwaitingVitals);
        _appointmentRepositoryMock.Verify(x => x.Update(appointment), Times.Once);
    }
}
