using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.TourClosure.Domain.Model.Aggregates;
using VitalTrek.API.TourClosure.Domain.Repositories;

namespace VitalTrek.API.TourClosure.Infrastructure.Persistence.EntityFrameworkCore.Repositories
{
    public class TourSummaryRepository : BaseRepository<TourSummary>, ITourSummaryRepository
    {
        public TourSummaryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<TourSummary?> FindByExpeditionGroupIdAsync(int expeditionGroupId)
        {
            return await Context.Set<TourSummary>()
                .FirstOrDefaultAsync(t => t.ExpeditionGroupId == expeditionGroupId);
        }

        public async Task<IEnumerable<TourSummary>> FindByAgencyIdAsync(int agencyId)
        {
            return await Context.Set<TourSummary>()
                .Where(t => t.AgencyId == agencyId)
                .ToListAsync();
        }
    }
}