using System.Collections.Generic;
using System.Threading.Tasks;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.TourClosure.Domain.Model.Aggregates;

namespace VitalTrek.API.TourClosure.Domain.Repositories
{
    public interface ITourSummaryRepository : IBaseRepository<TourSummary>
    {
        Task<TourSummary?> FindByExpeditionGroupIdAsync(int expeditionGroupId);
        Task<IEnumerable<TourSummary>> FindByAgencyIdAsync(int agencyId);
    }
}