using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckpointSyncContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checkpoint_passages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    session_id = table.Column<int>(type: "int", nullable: false),
                    checkpoint_id = table.Column<int>(type: "int", nullable: false),
                    field_guide_id = table.Column<int>(type: "int", nullable: false),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    passed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_checkpoint_passages", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_checkpoint_passages_agency_id",
                table: "checkpoint_passages",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "i_x_checkpoint_passages_checkpoint_id",
                table: "checkpoint_passages",
                column: "checkpoint_id");

            migrationBuilder.CreateIndex(
                name: "i_x_checkpoint_passages_field_guide_id",
                table: "checkpoint_passages",
                column: "field_guide_id");

            migrationBuilder.CreateIndex(
                name: "ux_checkpoint_passages_session_checkpoint",
                table: "checkpoint_passages",
                columns: new[] { "session_id", "checkpoint_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkpoint_passages");
        }
    }
}
