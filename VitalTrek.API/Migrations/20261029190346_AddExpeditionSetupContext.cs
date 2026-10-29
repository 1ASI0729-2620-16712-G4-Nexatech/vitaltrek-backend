using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddExpeditionSetupContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    origin = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    destination = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    distance_km = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    elevation_gain_meters = table.Column<int>(type: "int", nullable: false),
                    difficulty = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    estimated_duration_minutes = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_routes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_routes__agency_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "checkpoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    location = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    sequence_order = table.Column<int>(type: "int", nullable: false),
                    bluetooth_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: false),
                    longitude = table.Column<double>(type: "double", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_checkpoints", x => x.id);
                    table.ForeignKey(
                        name: "f_k_checkpoints__route_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "route_notes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    content = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_route_notes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_route_notes_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "safety_protocols",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    instructions = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_safety_protocols", x => x.id);
                    table.ForeignKey(
                        name: "f_k_safety_protocols_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "expected_time_windows",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    from_checkpoint_id = table.Column<int>(type: "int", nullable: false),
                    to_checkpoint_id = table.Column<int>(type: "int", nullable: false),
                    minimum_minutes = table.Column<int>(type: "int", nullable: false),
                    maximum_minutes = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_expected_time_windows", x => x.id);
                    table.ForeignKey(
                        name: "f_k_expected_time_windows__route_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_expected_time_windows_checkpoints_from_checkpoint_id",
                        column: x => x.from_checkpoint_id,
                        principalTable: "checkpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_expected_time_windows_checkpoints_to_checkpoint_id",
                        column: x => x.to_checkpoint_id,
                        principalTable: "checkpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_checkpoints_agency_id",
                table: "checkpoints",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_checkpoints_bluetooth_id",
                table: "checkpoints",
                column: "bluetooth_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_checkpoints_route_id_sequence_order",
                table: "checkpoints",
                columns: new[] { "route_id", "sequence_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_expected_time_windows_agency_id",
                table: "expected_time_windows",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_expected_time_windows_from_checkpoint_id",
                table: "expected_time_windows",
                column: "from_checkpoint_id");

            migrationBuilder.CreateIndex(
                name: "i_x_expected_time_windows_to_checkpoint_id",
                table: "expected_time_windows",
                column: "to_checkpoint_id");

            migrationBuilder.CreateIndex(
                name: "ux_expected_time_windows_segment",
                table: "expected_time_windows",
                columns: new[] { "route_id", "from_checkpoint_id", "to_checkpoint_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_route_notes_agency_id",
                table: "route_notes",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_route_notes_route_id_display_order",
                table: "route_notes",
                columns: new[] { "route_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_routes_agency_id_name",
                table: "routes",
                columns: new[] { "agency_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_safety_protocols_agency_id",
                table: "safety_protocols",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_safety_protocols_route_id",
                table: "safety_protocols",
                column: "route_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expected_time_windows");

            migrationBuilder.DropTable(
                name: "route_notes");

            migrationBuilder.DropTable(
                name: "safety_protocols");

            migrationBuilder.DropTable(
                name: "checkpoints");

            migrationBuilder.DropTable(
                name: "routes");
        }
    }
}
