using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.IAM.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="Agency"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class AgencyRepository(AppDbContext context) : BaseRepository<Agency>(context), IAgencyRepository;
