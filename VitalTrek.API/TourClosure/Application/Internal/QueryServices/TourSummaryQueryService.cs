using System.Collections.Generic;
using System.Threading.Tasks;
using VitalTrek.API.TourClosure.Application.Services;
using VitalTrek.API.TourClosure.Domain.Model.Aggregates;
using VitalTrek.API.TourClosure.Domain.Model.Queries;
using VitalTrek.API.TourClosure.Domain.Repositories;

namespace VitalTrek.API.TourClosure.Application.Internal.QueryServices
{
    public class TourSummaryQueryService : ITourSummaryQueryService
    {
        private readonly ITourSummaryRepository _tourSummaryRepository;

        public TourSummaryQueryService(ITourSummaryRepository tourSummaryRepository)
        {
            _tourSummaryRepository = tourSummaryRepository;
        }

        public async Task<TourSummary?> Handle(GetTourSummaryByIdQuery query)
        {
            return await _tourSummaryRepository.FindByIdAsync(query.Id);
        }

        public async Task<TourSummary?> Handle(GetTourSummaryByGroupIdQuery query)
        {
            return await _tourSummaryRepository.FindByExpeditionGroupIdAsync(query.ExpeditionGroupId);
        }

        public async Task<IEnumerable<TourSummary>> Handle(GetAllTourSummariesByAgencyIdQuery query)
        {
            return await _tourSummaryRepository.FindByAgencyIdAsync(query.AgencyId);
        }
    }
}