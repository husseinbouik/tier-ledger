using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TierLedger.Domain.Common;
using TierLedger.Infrastructure.Persistence;

namespace TierLedger.Infrastructure.Outbox;

/// <summary>
/// Polls the outbox and publishes each stored domain event through MediatR.
/// Delivery is at-least-once: a message is only marked processed after its
/// handlers complete, and handlers are written to be idempotent. A failing
/// message keeps its error and is retried on the next poll; it does not block
/// the rest of the batch.
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeSpan _pollInterval;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher started (poll every {PollInterval}).", _pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch batch failed; will retry on the next poll.");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Unknown domain event type '{message.Type}'.");

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as IDomainEvent
                    ?? throw new InvalidOperationException($"Could not deserialize outbox message '{message.Id}'.");

                await mediator.Publish(domainEvent, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                _logger.LogError(
                    ex,
                    "Failed to dispatch outbox message {MessageId} ({EventType}); it will be retried.",
                    message.Id,
                    message.Type);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
