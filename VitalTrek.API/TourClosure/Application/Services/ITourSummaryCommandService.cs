using System.Threading.Tasks;
using VitalTrek.API.TourClosure.Domain.Model.Aggregates;
using VitalTrek.API.TourClosure.Domain.Model.Commands;

namespace VitalTrek.API.TourClosure.Application.Services
{
    public interface ITourSummaryCommandService
    {
        Task<TourSummary?> Handle(CloseTourCommand command);
        Task<TourSummary?> Handle(PublishTourSummaryCommand command);
    }
}