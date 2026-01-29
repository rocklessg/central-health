using CentralHealth.Application.Interfaces;
using CentralHealth.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CentralHealth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IRecordsService, RecordsService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
