using Microsoft.EntityFrameworkCore;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.SafetyMonitoring.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="EarlyWarningAlert"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class EarlyWarningAlertRepository(AppDbContext context)
    : BaseRepository<EarlyWarningAlert>(context), IEarlyWarningAlertRepository
{
    /// <inheritdoc />
    public async Task<EarlyWarningAlert?> FindByIdAndAgencyIdAsync(int alertId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<EarlyWarningAlert>().FirstOrDefaultAsync(
            alert => alert.Id == alertId && alert.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<EarlyWarningAlert>> FindOpenByAgencyIdAsync(int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<EarlyWarningAlert>()
            .Where(alert => alert.AgencyId == agencyId && (alert.Status == AlertStatus.Active || alert.Status == AlertStatus.Acknowledged))
            .OrderByDescending(alert => alert.RaisedAt).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<EarlyWarningAlert>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<EarlyWarningAlert>()
            .Where(alert => alert.SessionId == sessionId && alert.AgencyId == agencyId)
            .OrderByDescending(alert => alert.RaisedAt).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsBySessionIdCheckpointIdAndTypeAsync(int sessionId, int checkpointId, AlertType alertType,
        CancellationToken cancellationToken = default) =>
        await Context.Set<EarlyWarningAlert>().AnyAsync(alert =>
            alert.SessionId == sessionId && alert.CheckpointId == checkpointId && alert.AlertType == alertType,
            cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasOpenBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await Context.Set<EarlyWarningAlert>().AnyAsync(
            alert => alert.SessionId == sessionId && (alert.Status == AlertStatus.Active || alert.Status == AlertStatus.Acknowledged), cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountCriticalBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await Context.Set<EarlyWarningAlert>().CountAsync(alert =>
            alert.SessionId == sessionId && alert.Severity == AlertSeverity.Critical &&
            alert.Status != AlertStatus.Dismissed, cancellationToken);
}
