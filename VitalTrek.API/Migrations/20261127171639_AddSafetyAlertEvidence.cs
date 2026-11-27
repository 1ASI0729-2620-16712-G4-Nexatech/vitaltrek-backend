using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyAlertEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "acknowledged_at",
                table: "early_warning_alerts",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "acknowledged_by_user_id",
                table: "early_warning_alerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "early_warning_alerts",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "closed_by_user_id",
                table: "early_warning_alerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "evidence_expected_maximum",
                table: "early_warning_alerts",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "evidence_expected_minimum",
                table: "early_warning_alerts",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_key",
                table: "early_warning_alerts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "evidence_observed_value",
                table: "early_warning_alerts",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "evidence_recorded_at",
                table: "early_warning_alerts",
                type: "datetime(6)",
                precision: 6,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "evidence_simulated",
                table: "early_warning_alerts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "evidence_source_record_id",
                table: "early_warning_alerts",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_unit",
                table: "early_warning_alerts",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "member_id",
                table: "early_warning_alerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rule",
                table: "early_warning_alerts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            // Alerts stored before evidence existed were raised manually: give each one a unique legacy evidence key
            // and the manual rule, so the unique index can be created and the rows remain readable.
            migrationBuilder.Sql(
                "UPDATE early_warning_alerts SET `rule` = 'manual' WHERE `rule` = '';");
            migrationBuilder.Sql(
                "UPDATE early_warning_alerts SET evidence_key = CONCAT('legacy:', id) WHERE evidence_key = '';");

            migrationBuilder.CreateIndex(
                name: "ix_early_warning_alerts_session_evidence_key",
                table: "early_warning_alerts",
                columns: new[] { "session_id", "evidence_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_early_warning_alerts_session_evidence_key",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "acknowledged_at",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "acknowledged_by_user_id",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "closed_by_user_id",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_expected_maximum",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_expected_minimum",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_key",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_observed_value",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_recorded_at",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_simulated",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_source_record_id",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "evidence_unit",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "member_id",
                table: "early_warning_alerts");

            migrationBuilder.DropColumn(
                name: "rule",
                table: "early_warning_alerts");
        }
    }
}
