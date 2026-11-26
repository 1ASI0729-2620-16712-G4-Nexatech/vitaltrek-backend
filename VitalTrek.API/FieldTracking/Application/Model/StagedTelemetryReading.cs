using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;

namespace VitalTrek.API.FieldTracking.Application.Model;

public sealed record StagedTelemetryReading(TelemetryRecord Record, bool IsDuplicate);
