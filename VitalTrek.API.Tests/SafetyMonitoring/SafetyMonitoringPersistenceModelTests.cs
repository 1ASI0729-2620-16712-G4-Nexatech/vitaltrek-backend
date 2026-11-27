using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring;

/// <summary>
///     Checks the Entity Framework Core mapping of the alert evidence without a database: the evidence is stored in flat
///     snake_case columns, its projection is not mapped and the database keeps one alert per evidence and session.
/// </summary>
public class SafetyMonitoringPersistenceModelTests
{
    private static readonly IEntityType Alert = BuildAlertEntityType();

    private static IEntityType BuildAlertEntityType()
    {
        // Building the model does not open a connection; the connection string only selects the MySQL provider.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("server=localhost;database=vitaltrek_model_only").Options;
        using var context = new AppDbContext(options);
        return context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(EarlyWarningAlert))!;
    }

    [Fact]
    public void EvidenceProjectionIsNotMapped()
    {
        Assert.Null(Alert.FindProperty(nameof(EarlyWarningAlert.Evidence)));
        Assert.Null(Alert.FindNavigation(nameof(EarlyWarningAlert.Evidence)));
    }

    [Theory]
    [InlineData(nameof(EarlyWarningAlert.Rule), "rule")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceKey), "evidence_key")]
    [InlineData(nameof(EarlyWarningAlert.MemberId), "member_id")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceObservedValue), "evidence_observed_value")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceExpectedMinimum), "evidence_expected_minimum")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceExpectedMaximum), "evidence_expected_maximum")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceUnit), "evidence_unit")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceRecordedAt), "evidence_recorded_at")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceSourceRecordId), "evidence_source_record_id")]
    [InlineData(nameof(EarlyWarningAlert.EvidenceSimulated), "evidence_simulated")]
    [InlineData(nameof(EarlyWarningAlert.AcknowledgedByUserId), "acknowledged_by_user_id")]
    [InlineData(nameof(EarlyWarningAlert.AcknowledgedAt), "acknowledged_at")]
    [InlineData(nameof(EarlyWarningAlert.ClosedByUserId), "closed_by_user_id")]
    [InlineData(nameof(EarlyWarningAlert.ClosedAt), "closed_at")]
    public void EvidenceIsStoredInSnakeCaseColumns(string property, string column)
    {
        Assert.Equal(column, Alert.FindProperty(property)!.GetColumnName());
    }

    [Fact]
    public void RuleAndEvidenceIdentityAreRequiredCodes()
    {
        var rule = Alert.FindProperty(nameof(EarlyWarningAlert.Rule))!;
        Assert.False(rule.IsNullable);
        Assert.Equal(typeof(string), rule.GetValueConverter()!.ProviderClrType);
        Assert.Equal(40, rule.GetMaxLength());

        var evidenceKey = Alert.FindProperty(nameof(EarlyWarningAlert.EvidenceKey))!;
        Assert.False(evidenceKey.IsNullable);
        Assert.Equal(EarlyWarningAlert.EvidenceKeyMaxLength, evidenceKey.GetMaxLength());
    }

    [Fact]
    public void EvidenceKeepsTheCapturePrecisionAndTheClientIdentityFormat()
    {
        Assert.Equal(6, Alert.FindProperty(nameof(EarlyWarningAlert.EvidenceRecordedAt))!.GetPrecision());
        Assert.Equal("char(36)", Alert.FindProperty(nameof(EarlyWarningAlert.EvidenceSourceRecordId))!.GetColumnType());
        var observed = Alert.FindProperty(nameof(EarlyWarningAlert.EvidenceObservedValue))!;
        Assert.Equal(10, observed.GetPrecision());
        Assert.Equal(2, observed.GetScale());
    }

    [Fact]
    public void DatabaseKeepsOneAlertPerEvidenceAndSession()
    {
        var index = Assert.Single(Alert.GetIndexes(),
            candidate => candidate.GetDatabaseName() == "ix_early_warning_alerts_session_evidence_key");
        Assert.True(index.IsUnique);
        Assert.Equal(new[] { nameof(EarlyWarningAlert.SessionId), nameof(EarlyWarningAlert.EvidenceKey) },
            index.Properties.Select(property => property.Name));
    }
}
