using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryCaptureIdentityAndMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Explicit SQL avoids the provider synthesizing a DateTimeOffset.MinValue default,
            // which is outside MySQL's datetime range. The existing timestamps are preserved.
            migrationBuilder.Sql("""
                ALTER TABLE `telemetry_records`
                CHANGE COLUMN `synced_at` `synchronized_at` datetime(6) NOT NULL,
                MODIFY COLUMN `recorded_at` datetime(6) NOT NULL;
                """);

            migrationBuilder.AddColumn<int>(
                name: "battery",
                table: "telemetry_records",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "client_record_id",
                table: "telemetry_records",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "deviation_meters",
                table: "telemetry_records",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "heart_rate",
                table: "telemetry_records",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "member_id",
                table: "telemetry_records",
                type: "int",
                nullable: true);

            // Preserve historical captures with deterministic, unique identities. Health measurements and
            // participant identity remain NULL because their original values cannot be inferred reliably.
            migrationBuilder.Sql("""
                UPDATE `telemetry_records`
                SET `client_record_id` = CONCAT('00000000-0000-0000-0000-', LPAD(HEX(`id`), 12, '0'))
                WHERE `client_record_id` IS NULL;
                """);
            migrationBuilder.Sql("ALTER TABLE `telemetry_records` MODIFY COLUMN `client_record_id` char(36) NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "i_x_telemetry_records_member_id",
                table: "telemetry_records",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ux_telemetry_records_session_client_record",
                table: "telemetry_records",
                columns: new[] { "session_id", "client_record_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "f_k_telemetry_records_manifest_entries_member_id",
                table: "telemetry_records",
                column: "member_id",
                principalTable: "manifest_entries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_telemetry_records_manifest_entries_member_id",
                table: "telemetry_records");

            migrationBuilder.DropIndex(
                name: "i_x_telemetry_records_member_id",
                table: "telemetry_records");

            migrationBuilder.DropIndex(
                name: "ux_telemetry_records_session_client_record",
                table: "telemetry_records");

            migrationBuilder.DropColumn(
                name: "battery",
                table: "telemetry_records");

            migrationBuilder.DropColumn(
                name: "client_record_id",
                table: "telemetry_records");

            migrationBuilder.DropColumn(
                name: "deviation_meters",
                table: "telemetry_records");

            migrationBuilder.DropColumn(
                name: "heart_rate",
                table: "telemetry_records");

            migrationBuilder.DropColumn(
                name: "member_id",
                table: "telemetry_records");

            migrationBuilder.Sql("""
                ALTER TABLE `telemetry_records`
                CHANGE COLUMN `synchronized_at` `synced_at` datetime NOT NULL,
                MODIFY COLUMN `recorded_at` datetime NOT NULL;
                """);

        }
    }
}
