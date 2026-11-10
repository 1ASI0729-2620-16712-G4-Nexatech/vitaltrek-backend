using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldTrackingBoundedContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tracking_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    expedition_group_id = table.Column<int>(type: "int", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_tracking_sessions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_tracking_sessions__agency_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_tracking_sessions_expedition_groups_expedition_group_id",
                        column: x => x.expedition_group_id,
                        principalTable: "expedition_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "telemetry_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    session_id = table.Column<int>(type: "int", nullable: false),
                    wearable_device_id = table.Column<int>(type: "int", nullable: false),
                    latitude = table.Column<double>(type: "double", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<double>(type: "double", precision: 10, scale: 7, nullable: false),
                    altitude = table.Column<double>(type: "double", precision: 10, scale: 2, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_telemetry_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_telemetry_records__tracking_session_session_id",
                        column: x => x.session_id,
                        principalTable: "tracking_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_telemetry_records_session_id",
                table: "telemetry_records",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_records_session_wearable_time",
                table: "telemetry_records",
                columns: new[] { "session_id", "wearable_device_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_tracking_sessions_agency_id",
                table: "tracking_sessions",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracking_sessions_group_status",
                table: "tracking_sessions",
                columns: new[] { "expedition_group_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry_records");

            migrationBuilder.DropTable(
                name: "tracking_sessions");
        }
    }
}
