using Microsoft.EntityFrameworkCore;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.Processes;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.Common.Interfaces;

/// <summary>
/// The application's view of persistence. Handlers depend on this abstraction;
/// the EF Core implementation lives in the Infrastructure layer.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<ThirdParty> ThirdParties { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<PaymentProcess> PaymentProcesses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
