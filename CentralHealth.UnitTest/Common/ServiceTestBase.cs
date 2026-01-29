using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace CentralHealth.UnitTest.Common;

public abstract class ServiceTestBase<TService>
{
    protected Mock<IUnitOfWork> UnitOfWorkMock { get; }
    protected Mock<IValidationService> ValidationServiceMock { get; }

    protected ServiceTestBase()
    {
        UnitOfWorkMock = new Mock<IUnitOfWork>();
        ValidationServiceMock = new Mock<IValidationService>();

        // Default: validation passes
        ValidationServiceMock
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, new List<string>()));
    }

    protected Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    protected Mock<IRepository<T>> CreateRepositoryMock<T>() where T : class
    {
        return new Mock<IRepository<T>>();
    }

    protected void SetupValidationFailure(params string[] errors)
    {
        ValidationServiceMock
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errors.ToList()));
    }

    protected void SetupValidationSuccess()
    {
        ValidationServiceMock
            .Setup(x => x.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, new List<string>()));
    }
}
