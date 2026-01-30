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

    #region ProcessPaymentAsync Tests

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
        result.Message.Should().Contain("Invoice not found");
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
        result.Message.Should().Contain("Invoice is already paid");
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
        result.Message.Should().Contain("exceeds outstanding amount");
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
        result.Message.Should().Contain("Insufficient wallet balance");
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

    [Fact]
    public async Task ProcessPaymentAsync_WhenValidationFails_ReturnsFailure()
    {
        // Arrange
        SetupValidationFailure("Amount must be greater than zero", "Invoice ID is required");

        var request = new ProcessPaymentRequest
        {
            FacilityId = TestDataFactory.DefaultFacilityId,
            Username = TestDataFactory.DefaultUsername,
            InvoiceId = Guid.NewGuid(),
            Amount = 0,
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Amount must be greater than zero");
        result.Errors.Should().Contain("Invoice ID is required");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenInvoiceCancelled_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(
            facilityId: facilityId,
            totalAmount: 5000,
            status: InvoiceStatus.Cancelled);
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
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot process payment for a cancelled invoice");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithWalletPaymentAndNullWallet_ReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        patient.Wallet = null; // No wallet
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
        result.Message.Should().Contain("Insufficient wallet balance");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithCardPayment_CreatesPayment()
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
            Method = PaymentMethod.Card,
            TransactionId = "TXN-12345"
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Method.Should().Be("Card");
        result.Data.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithBankTransferPayment_CreatesPayment()
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
            Method = PaymentMethod.BankTransfer,
            TransactionId = "BANK-TXN-67890",
            Notes = "Payment via bank transfer"
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Method.Should().Be("BankTransfer");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenFullPaymentWithAppointmentNotAwaitingPayment_DoesNotUpdateAppointment()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var appointment = TestDataFactory.CreateAppointment(
            facilityId: facilityId,
            status: AppointmentStatus.Scheduled); // Not AwaitingPayment
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
            Amount = 5000,
            Method = PaymentMethod.Cash
        };

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Scheduled); // Should remain unchanged
        _appointmentRepositoryMock.Verify(x => x.Update(appointment), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenExceptionOccurs_RollsBackTransactionAndReturnsFailure()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var patient = TestDataFactory.CreatePatient(facilityId: facilityId);
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId, totalAmount: 5000);
        invoice.Patient = patient;

        var invoices = new List<Invoice> { invoice };
        var mockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        _paymentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

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
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("An error occurred while processing the payment");
        UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_VerifiesTransactionCommit()
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
        UnitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_PaymentReferenceIsGenerated()
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
        result.Data!.PaymentReference.Should().StartWith("PAY-");
        result.Data.PaymentReference.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetPaymentByIdAsync Tests

    [Fact]
    public async Task GetPaymentByIdAsync_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId);
        var payment = TestDataFactory.CreatePayment(invoiceId: invoice.Id, amount: 5000);
        payment.Invoice = invoice;

        var payments = new List<Payment> { payment };
        var mockQueryable = payments.AsQueryable().BuildMock();
        _paymentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        // Act
        var result = await _service.GetPaymentByIdAsync(payment.Id, facilityId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(payment.Id);
        result.Data.Amount.Should().Be(5000);
        result.Data.Method.Should().Be("Cash");
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WhenPaymentNotFound_ReturnsFailure()
    {
        // Arrange
        var payments = new List<Payment>();
        var mockQueryable = payments.AsQueryable().BuildMock();
        _paymentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        // Act
        var result = await _service.GetPaymentByIdAsync(Guid.NewGuid(), TestDataFactory.DefaultFacilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Payment not found");
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WhenPaymentFromDifferentFacility_ReturnsFailure()
    {
        // Arrange
        var otherFacilityId = Guid.NewGuid();
        var invoice = TestDataFactory.CreateInvoice(facilityId: otherFacilityId);
        var payment = TestDataFactory.CreatePayment(invoiceId: invoice.Id);
        payment.Invoice = invoice;

        var payments = new List<Payment> { payment };
        var mockQueryable = payments.AsQueryable().BuildMock();
        _paymentRepositoryMock.Setup(x => x.Query()).Returns(mockQueryable);

        // Act
        var result = await _service.GetPaymentByIdAsync(payment.Id, TestDataFactory.DefaultFacilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Payment not found");
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WhenExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        _paymentRepositoryMock
            .Setup(x => x.Query())
            .Throws(new Exception("Database error"));

        // Act
        var result = await _service.GetPaymentByIdAsync(Guid.NewGuid(), TestDataFactory.DefaultFacilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("An error occurred while retrieving the payment");
    }

    #endregion

    #region GetPaymentsByInvoiceIdAsync Tests

    [Fact]
    public async Task GetPaymentsByInvoiceIdAsync_WhenInvoiceExistsWithPayments_ReturnsPayments()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId);
        var payment1 = TestDataFactory.CreatePayment(invoiceId: invoice.Id, amount: 2000);
        var payment2 = TestDataFactory.CreatePayment(invoiceId: invoice.Id, amount: 3000);
        payment2.PaymentDate = DateTime.UtcNow.AddMinutes(5);

        var invoices = new List<Invoice> { invoice };
        var invoiceMockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(invoiceMockQueryable);

        var payments = new List<Payment> { payment1, payment2 };
        var paymentMockQueryable = payments.AsQueryable().BuildMock();
        _paymentRepositoryMock.Setup(x => x.Query()).Returns(paymentMockQueryable);

        // Act
        var result = await _service.GetPaymentsByInvoiceIdAsync(invoice.Id, facilityId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPaymentsByInvoiceIdAsync_WhenInvoiceExistsWithNoPayments_ReturnsEmptyList()
    {
        // Arrange
        var facilityId = TestDataFactory.DefaultFacilityId;
        var invoice = TestDataFactory.CreateInvoice(facilityId: facilityId);

        var invoices = new List<Invoice> { invoice };
        var invoiceMockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(invoiceMockQueryable);

        var payments = new List<Payment>();
        var paymentMockQueryable = payments.AsQueryable().BuildMock();
        _paymentRepositoryMock.Setup(x => x.Query()).Returns(paymentMockQueryable);

        // Act
        var result = await _service.GetPaymentsByInvoiceIdAsync(invoice.Id, facilityId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentsByInvoiceIdAsync_WhenInvoiceNotFound_ReturnsFailure()
    {
        // Arrange
        var invoices = new List<Invoice>();
        var invoiceMockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(invoiceMockQueryable);

        // Act
        var result = await _service.GetPaymentsByInvoiceIdAsync(Guid.NewGuid(), TestDataFactory.DefaultFacilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invoice not found");
    }

    [Fact]
    public async Task GetPaymentsByInvoiceIdAsync_WhenInvoiceFromDifferentFacility_ReturnsFailure()
    {
        // Arrange
        var otherFacilityId = Guid.NewGuid();
        var invoice = TestDataFactory.CreateInvoice(facilityId: otherFacilityId);

        var invoices = new List<Invoice> { invoice };
        var invoiceMockQueryable = invoices.AsQueryable().BuildMock();
        _invoiceRepositoryMock.Setup(x => x.Query()).Returns(invoiceMockQueryable);

        // Act
        var result = await _service.GetPaymentsByInvoiceIdAsync(invoice.Id, TestDataFactory.DefaultFacilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invoice not found");
    }

    [Fact]
    public async Task GetPaymentsByInvoiceIdAsync_WhenExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        _invoiceRepositoryMock
            .Setup(x => x.Query())
            .Throws(new Exception("Database error"));

        // Act
        var result = await _service.GetPaymentsByInvoiceIdAsync(Guid.NewGuid(), TestDataFactory.DefaultFacilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("An error occurred while retrieving payments");
    }

    #endregion
}
