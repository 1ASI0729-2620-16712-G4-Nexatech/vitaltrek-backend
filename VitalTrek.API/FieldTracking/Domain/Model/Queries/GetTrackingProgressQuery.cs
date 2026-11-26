namespace VitalTrek.API.FieldTracking.Domain.Model.Queries;

public sealed record GetTrackingProgressBySessionIdQuery(int AgencyId, int SessionId);
public sealed record GetTrackingProgressByGroupIdQuery(int AgencyId, int ExpeditionGroupId);
