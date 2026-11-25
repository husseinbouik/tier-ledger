using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using TierLedger.Api.Infrastructure;
using TierLedger.Application;
using TierLedger.Infrastructure;
using TierLedger.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TierLedger API",
        Version = "v1",
        Description = "Third-party (tiers) and supplier invoice lifecycle service: " +
                      "CQRS with MediatR, a payment process manager (saga) and the transactional outbox pattern.",
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await ApplyDatabaseSetupAsync(app);

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

// Swagger is part of the demo surface of this reference project, so it is
// enabled in every environment.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TierLedger API v1");
    options.RoutePrefix = "swagger";
});

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static async Task ApplyDatabaseSetupAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.IsSqlServer())
    {
        // Under docker-compose the database container may still be starting;
        // retry the migration instead of crashing the API.
        const int maxAttempts = 12;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                app.Logger.LogWarning(
                    "Database not ready (attempt {Attempt}/{MaxAttempts}): {Message}. Retrying in 5s…",
                    attempt,
                    maxAttempts,
                    ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
    else
    {
        // SQLite is the zero-dependency local/test provider; its schema is
        // created from the model. SQL Server migrations live in Infrastructure.
        await db.Database.EnsureCreatedAsync();
    }

    if (app.Environment.IsDevelopment())
        await DevelopmentSeeder.SeedAsync(scope.ServiceProvider);
}

public partial class Program;
