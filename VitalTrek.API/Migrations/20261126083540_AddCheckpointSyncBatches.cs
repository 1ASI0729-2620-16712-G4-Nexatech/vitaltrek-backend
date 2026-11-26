using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckpointSyncBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_batches",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    session_id = table.Column<int>(type: "int", nullable: false),
                    checkpoint_id = table.Column<int>(type: "int", nullable: false),
                    field_guide_id = table.Column<int>(type: "int", nullable: false),
                    client_batch_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    content_fingerprint = table.Column<string>(type: "char(64)", nullable: false),
                    received_at = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    evaluation_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_sync_batches", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sync_batch_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    sync_batch_id = table.Column<int>(type: "int", nullable: false),
                    client_record_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    outcome = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    rejection_code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    record_fingerprint = table.Column<string>(type: "char(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_sync_batch_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_sync_batch_records_sync_batches_sync_batch_id",
                        column: x => x.sync_batch_id,
                        principalTable: "sync_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_sync_batch_records_client_record",
                table: "sync_batch_records",
                column: "client_record_id");

            migrationBuilder.CreateIndex(
                name: "ux_sync_batch_records_batch_client_record",
                table: "sync_batch_records",
                columns: new[] { "sync_batch_id", "client_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_sync_batches_agency_id",
                table: "sync_batches",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ux_sync_batches_session_client_batch",
                table: "sync_batches",
                columns: new[] { "session_id", "client_batch_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_batch_records");

            migrationBuilder.DropTable(
                name: "sync_batches");
        }
    }
}
