using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VitalTrek.API.TourClosure.Application.Services;
using VitalTrek.API.TourClosure.Domain.Model.Commands;
using VitalTrek.API.TourClosure.Domain.Model.Queries;
using VitalTrek.API.TourClosure.Interfaces.Rest.Resources;
using VitalTrek.API.TourClosure.Interfaces.Rest.Transform;

namespace VitalTrek.API.TourClosure.Interfaces.Rest
{
    [ApiController]
    [Route("api/v1/tour-summaries")]
    [Produces(MediaTypeNames.Application.Json)]
    public class TourSummariesController : ControllerBase
    {
        private readonly ITourSummaryCommandService _commandService;
        private readonly ITourSummaryQueryService _queryService;

        public TourSummariesController(
            ITourSummaryCommandService commandService,
            ITourSummaryQueryService queryService)
        {
            _commandService = commandService;
            _queryService = queryService;
        }

        [HttpPost]
        public async Task<IActionResult> CloseTour([FromBody] CloseTourResource resource)
        {
            var command = new CloseTourCommand(
                resource.ExpeditionGroupId,
                resource.AgencyId,
                resource.StartedAt,
                resource.ClosedAt,
                resource.TotalParticipants,
                resource.CheckpointsReached,
                resource.AlertsRaised,
                resource.IncidentsReported,
                resource.Notes
            );

            var result = await _commandService.Handle(command);
            if (result == null) return BadRequest();

            var response = TourSummaryResourceFromEntityAssembler.ToResourceFromEntity(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
        }

        [HttpPost("{id:int}/publish")]
        public async Task<IActionResult> PublishTour(int id)
        {
            var command = new PublishTourSummaryCommand(id);
            var result = await _commandService.Handle(command);
            if (result == null) return NotFound();

            var response = TourSummaryResourceFromEntityAssembler.ToResourceFromEntity(result);
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var query = new GetTourSummaryByIdQuery(id);
            var result = await _queryService.Handle(query);
            if (result == null) return NotFound();

            return Ok(TourSummaryResourceFromEntityAssembler.ToResourceFromEntity(result));
        }

        [HttpGet("group/{groupId:int}")]
        public async Task<IActionResult> GetByGroupId(int groupId)
        {
            var query = new GetTourSummaryByGroupIdQuery(groupId);
            var result = await _queryService.Handle(query);
            if (result == null) return NotFound();

            return Ok(TourSummaryResourceFromEntityAssembler.ToResourceFromEntity(result));
        }

        [HttpGet("agency/{agencyId:int}")]
        public async Task<ActionResult<IEnumerable<TourSummaryResource>>> GetByAgencyId(int agencyId)
        {
            var query = new GetAllTourSummariesByAgencyIdQuery(agencyId);
            var results = await _queryService.Handle(query);
            var resources = results.Select(TourSummaryResourceFromEntityAssembler.ToResourceFromEntity);
            return Ok(resources);
        }
    }
}