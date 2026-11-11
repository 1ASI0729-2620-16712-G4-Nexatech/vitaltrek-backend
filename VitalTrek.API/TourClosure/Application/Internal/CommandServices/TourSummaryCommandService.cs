using System.Threading.Tasks;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.TourClosure.Application.Services;
using VitalTrek.API.TourClosure.Domain.Model.Aggregates;
using VitalTrek.API.TourClosure.Domain.Model.Commands;
using VitalTrek.API.TourClosure.Domain.Repositories;

namespace VitalTrek.API.TourClosure.Application.Internal.CommandServices
{
    public class TourSummaryCommandService : ITourSummaryCommandService
    {
        private readonly ITourSummaryRepository _tourSummaryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TourSummaryCommandService(ITourSummaryRepository tourSummaryRepository, IUnitOfWork unitOfWork)
        {
            _tourSummaryRepository = tourSummaryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<TourSummary?> Handle(CloseTourCommand command)
        {
            var summary = new TourSummary(
                command.ExpeditionGroupId,
                command.AgencyId,
                command.StartedAt,
                command.ClosedAt,
                command.TotalParticipants,
                command.CheckpointsReached,
                command.AlertsRaised,
                command.IncidentsReported,
                command.Notes
            );

            await _tourSummaryRepository.AddAsync(summary);
            await _unitOfWork.CompleteAsync();
            return summary;
        }

        public async Task<TourSummary?> Handle(PublishTourSummaryCommand command)
        {
            var summary = await _tourSummaryRepository.FindByIdAsync(command.TourSummaryId);
            if (summary == null) return null;

            summary.Publish();
            await _unitOfWork.CompleteAsync();
            return summary;
        }
    }
}