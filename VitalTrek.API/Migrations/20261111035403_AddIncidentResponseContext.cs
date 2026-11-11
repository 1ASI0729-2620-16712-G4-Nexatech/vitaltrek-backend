using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentResponseContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incident_notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    incident_id = table.Column<int>(type: "int", nullable: false),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    recipient_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    channel = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_incident_notifications", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    session_id = table.Column<int>(type: "int", nullable: false),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    reported_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    incident_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_incidents", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_incident_notifications_agency_id",
                table: "incident_notifications",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_incident_notifications_incident_id",
                table: "incident_notifications",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_agency_id",
                table: "incidents",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_agency_id_status",
                table: "incidents",
                columns: new[] { "agency_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_session_id",
                table: "incidents",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incident_notifications");

            migrationBuilder.DropTable(
                name: "incidents");
        }
    }
}
