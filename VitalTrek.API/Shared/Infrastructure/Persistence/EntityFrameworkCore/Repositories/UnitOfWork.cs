using Microsoft.EntityFrameworkCore;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Implements the unit of work pattern to manage atomic database transactions across repositories.
/// </summary>
/// <param name="context">The application database context.</param>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    /// <inheritdoc />
    public void DiscardChanges() => context.ChangeTracker.Clear();

    /// <inheritdoc />
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        // The MySQL provider can batch multiple writes into one command. Always require a transaction,
        // including for that batch, so a failed command cannot leave a group or session partially saved.
        var previousBehavior = context.Database.AutoTransactionBehavior;
        context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            context.Database.AutoTransactionBehavior = previousBehavior;
        }
    }
}
