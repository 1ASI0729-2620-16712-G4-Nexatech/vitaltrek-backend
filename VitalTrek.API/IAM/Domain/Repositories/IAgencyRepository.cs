using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.IAM.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="Agency"/> aggregates.
/// </summary>
public interface IAgencyRepository : IBaseRepository<Agency>;
