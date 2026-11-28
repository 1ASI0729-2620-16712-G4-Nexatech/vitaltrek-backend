using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource with the optional health information an adventure tourist shares for the expedition. Empty clears it.
/// </summary>
public record HealthInformationResource(
    [StringLength(500, ErrorMessage = "HealthNotesTooLong")] string? HealthNotes);
