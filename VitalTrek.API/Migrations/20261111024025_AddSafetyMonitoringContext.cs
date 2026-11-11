using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyMonitoringContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "early_warning_alerts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    session_id = table.Column<int>(type: "int", nullable: false),
                    expedition_group_id = table.Column<int>(type: "int", nullable: false),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    checkpoint_id = table.Column<int>(type: "int", nullable: true),
                    alert_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    raised_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_early_warning_alerts", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_early_warning_alerts_agency_id_status",
                table: "early_warning_alerts",
                columns: new[] { "agency_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_early_warning_alerts_expedition_group_id",
                table: "early_warning_alerts",
                column: "expedition_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_early_warning_alerts_session_checkpoint_type",
                table: "early_warning_alerts",
                columns: new[] { "session_id", "checkpoint_id", "alert_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "early_warning_alerts");
        }
    }
}
