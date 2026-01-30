using CentralHealth.Application.DTOs.Facilities;
using CentralHealth.Application.Interfaces;
using CentralHealth.Application.Services;
using CentralHealth.Domain.Entities;
using CentralHealth.UnitTest.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;

namespace CentralHealth.UnitTest.Services;

public class FacilityServiceTests : ServiceTestBase<FacilityService>
{
    private readonly Mock<IRepository<Facility>> _facilityRepositoryMock;
    private readonly Mock<ILogger<FacilityService>> _loggerMock;
    private readonly FacilityService _service;

    public FacilityServiceTests()
    {
        _facilityRepositoryMock = CreateRepositoryMock<Facility>();
        _loggerMock = CreateLoggerMock<FacilityService>();
        _service = new FacilityService(
            _facilityRepositoryMock.Object,
            UnitOfWorkMock.Object,
            ValidationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateFacilityAsync_WithValidRequest_CreatesFacility()
    {
        // Arrange
        var request = new CreateFacilityRequest
        {
            Name = "Test Hospital",
            Code = "TH-001",
            Address = "123 Test Street",
            Phone = "+234-800-000-0000",
            Email = "test@hospital.com"
        };
        _facilityRepositoryMock.Setup(x => x.Query()).Returns(new List<Facility>().AsQueryable().BuildMock());
        // Act
        var result = await _service.CreateFacilityAsync(request);
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Test Hospital");
        result.Data.Code.Should().Be("TH-001");
    }

    [Fact]
    public async Task CreateFacilityAsync_WithDuplicateCode_ReturnsFailure()
    {
        // Arrange
        var existing = TestDataFactory.CreateFacility();
        _facilityRepositoryMock.Setup(x => x.Query()).Returns(new List<Facility> { existing }.AsQueryable().BuildMock());
        var request = new CreateFacilityRequest
        {
            Name = "Another Hospital",
            Code = existing.Code,
            Address = "456 New Street",
            Phone = "+234-800-000-0001",
            Email = "another@hospital.com"
        };
        // Act
        var result = await _service.CreateFacilityAsync(request);
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Facility code already exists");
    }

    [Fact]
    public async Task GetFacilityByIdAsync_WithValidId_ReturnsFacility()
    {
        // Arrange
        var facility = TestDataFactory.CreateFacility();
        _facilityRepositoryMock.Setup(x => x.Query()).Returns(new List<Facility> { facility }.AsQueryable().BuildMock());
        // Act
        var result = await _service.GetFacilityByIdAsync(facility.Id);
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(facility.Id);
    }
}
