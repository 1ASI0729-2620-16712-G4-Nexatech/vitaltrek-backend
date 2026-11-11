using System.Collections.Generic;
using System.Threading.Tasks;
using VitalTrek.API.TourClosure.Domain.Model.Aggregates;
using VitalTrek.API.TourClosure.Domain.Model.Queries;

namespace VitalTrek.API.TourClosure.Application.Services
{
    public interface ITourSummaryQueryService
    {
        Task<TourSummary?> Handle(GetTourSummaryByIdQuery query);
        Task<TourSummary?> Handle(GetTourSummaryByGroupIdQuery query);
        Task<IEnumerable<TourSummary>> Handle(GetAllTourSummariesByAgencyIdQuery query);
    }
}