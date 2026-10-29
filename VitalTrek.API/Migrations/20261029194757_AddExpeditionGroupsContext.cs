using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddExpeditionGroupsContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "field_guides",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_field_guides", x => x.id);
                    table.ForeignKey(
                        name: "f_k_field_guides__agency_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wearable_devices",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_wearable_devices", x => x.id);
                    table.ForeignKey(
                        name: "f_k_wearable_devices__agency_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "expedition_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    route_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    departure_date = table.Column<DateTime>(type: "date", nullable: false),
                    maximum_capacity = table.Column<int>(type: "int", nullable: false),
                    field_guide_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_expedition_groups", x => x.id);
                    table.ForeignKey(
                        name: "f_k_expedition_groups__agency_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_expedition_groups__field_guide_field_guide_id",
                        column: x => x.field_guide_id,
                        principalTable: "field_guides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_expedition_groups__route_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "manifest_entries",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    expedition_group_id = table.Column<int>(type: "int", nullable: false),
                    full_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    identity_document = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_manifest_entries", x => x.id);
                    table.ForeignKey(
                        name: "f_k_manifest_entries_expedition_groups_expedition_group_id",
                        column: x => x.expedition_group_id,
                        principalTable: "expedition_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "participant_preparations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    expedition_group_id = table.Column<int>(type: "int", nullable: false),
                    member_id = table.Column<int>(type: "int", nullable: false),
                    wearable_id = table.Column<int>(type: "int", nullable: true),
                    consent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    consent_recorded_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    activated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    emergency_contact_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: true),
                    emergency_contact_relationship = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true),
                    baseline_min = table.Column<int>(type: "int", nullable: false),
                    baseline_max = table.Column<int>(type: "int", nullable: false),
                    returned_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    return_condition = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_participant_preparations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_participant_preparations__wearable_device_wearable_id",
                        column: x => x.wearable_id,
                        principalTable: "wearable_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_participant_preparations_expedition_groups_expedition_group",
                        column: x => x.expedition_group_id,
                        principalTable: "expedition_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_participant_preparations_manifest_entries_member_id",
                        column: x => x.member_id,
                        principalTable: "manifest_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_expedition_groups_agency_id",
                table: "expedition_groups",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_expedition_groups_field_guide_id_departure_date",
                table: "expedition_groups",
                columns: new[] { "field_guide_id", "departure_date" });

            migrationBuilder.CreateIndex(
                name: "i_x_expedition_groups_route_id",
                table: "expedition_groups",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "i_x_field_guides_agency_id",
                table: "field_guides",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_manifest_entries_agency_id_identity_document",
                table: "manifest_entries",
                columns: new[] { "agency_id", "identity_document" });

            migrationBuilder.CreateIndex(
                name: "ux_manifest_entries_group_document",
                table: "manifest_entries",
                columns: new[] { "expedition_group_id", "identity_document" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_participant_preparations_expedition_group_id",
                table: "participant_preparations",
                column: "expedition_group_id");

            migrationBuilder.CreateIndex(
                name: "i_x_participant_preparations_member_id",
                table: "participant_preparations",
                column: "member_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_participant_preparations_wearable_id",
                table: "participant_preparations",
                column: "wearable_id");

            migrationBuilder.CreateIndex(
                name: "i_x_wearable_devices_agency_id",
                table: "wearable_devices",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_wearable_devices_code",
                table: "wearable_devices",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participant_preparations");

            migrationBuilder.DropTable(
                name: "wearable_devices");

            migrationBuilder.DropTable(
                name: "manifest_entries");

            migrationBuilder.DropTable(
                name: "expedition_groups");

            migrationBuilder.DropTable(
                name: "field_guides");
        }
    }
}
