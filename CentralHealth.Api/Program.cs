using CentralHealth.Api.Extensions;
using CentralHealth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully");
        
        // Seed only in development environment
        if (app.Environment.IsDevelopment())
        {
            await DbSeeder.SeedAsync(context);
            logger.LogInformation("Database seeding completed");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration");
    }
}

app.ConfigureMiddleware();

app.Run();
