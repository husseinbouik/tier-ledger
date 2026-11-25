using MediatR;
using Microsoft.Extensions.Logging;
using TierLedger.Domain.Common;

namespace TierLedger.Application.Common.EventHandlers;

/// <summary>
/// Receives every domain event (MediatR dispatches notifications to handlers
/// of base types as well) and writes an audit log line. In a production system
/// this is where an audit trail or an external event bus publisher would hook in.
/// </summary>
public sealed class AuditDomainEventHandler : INotificationHandler<IDomainEvent>
{
    private readonly ILogger<AuditDomainEventHandler> _logger;

    public AuditDomainEventHandler(ILogger<AuditDomainEventHandler> logger) => _logger = logger;

    public Task Handle(IDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain event: {DomainEvent}", notification.GetType().Name);
        return Task.CompletedTask;
    }
}
