using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="FieldGuide"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class FieldGuideRepository(AppDbContext context) : BaseRepository<FieldGuide>(context), IFieldGuideRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<FieldGuide>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default) =>
        await Context.Set<FieldGuide>().Where(guide => guide.AgencyId == agencyId).OrderBy(guide => guide.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<FieldGuide?> FindByIdAndAgencyIdAsync(int fieldGuideId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<FieldGuide>().FirstOrDefaultAsync(guide => guide.Id == fieldGuideId && guide.AgencyId == agencyId,
            cancellationToken);
}
