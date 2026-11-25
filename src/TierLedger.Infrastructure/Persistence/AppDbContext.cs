using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Common;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.Processes;
using TierLedger.Domain.ThirdParties;
using TierLedger.Infrastructure.Outbox;

namespace TierLedger.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ThirdParty> ThirdParties => Set<ThirdParty>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<PaymentProcess> PaymentProcesses => Set<PaymentProcess>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences => Set<InvoiceNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Every Guid key in this model is assigned by the domain factories
        // (Guid.NewGuid() in the Create methods), never by the store. Marking
        // them ValueGenerated.Never matters for change detection: with the
        // default store-generated convention, EF Core treats a child entity
        // discovered in a tracked aggregate's collection (e.g. a Payment added
        // by Invoice.RegisterPayment) as an existing row and issues an UPDATE
        // instead of an INSERT.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                         .Where(p => p.IsPrimaryKey() && p.ClrType == typeof(Guid)))
            {
                property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }
    }

    /// <summary>
    /// Transactional outbox: before saving, every domain event collected by the
    /// tracked aggregates is converted into an outbox row, so events and state
    /// changes commit (or roll back) together.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var domainEvent in domainEvents)
        {
            OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = DateTime.UtcNow,
            });
        }

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        return await base.SaveChangesAsync(cancellationToken);
    }
}
