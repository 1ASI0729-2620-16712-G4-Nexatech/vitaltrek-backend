using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class IncidentWorkflowIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The MySQL provider renders RenameColumn as a CHANGE that restates the column type, and the
            // restated type does not match the one MySQL 8 reports for this column, so the statement is rejected.
            // RENAME COLUMN is native to MySQL 8 and keeps the declared type untouched.
            migrationBuilder.Sql(
                "ALTER TABLE incident_notifications RENAME COLUMN sent_at TO requested_at;");

            migrationBuilder.AddColumn<string>(
                name: "health_notes",
                table: "participant_preparations",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "health_notes_recorded_at",
                table: "participant_preparations",
                type: "datetime",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "incidents",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "alert_id",
                table: "incidents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "incidents",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "confirmed_at",
                table: "incidents",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dismissed_at",
                table: "incidents",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "emergency_declared_at",
                table: "incidents",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "expedition_group_id",
                table: "incidents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "reported_by_user_id",
                table: "incidents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "accepted_at",
                table: "incident_notifications",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "incident_notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "body",
                table: "incident_notifications",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivered_at",
                table: "incident_notifications",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "incident_notifications",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_code",
                table: "incident_notifications",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_attempt_finished_at",
                table: "incident_notifications",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_attempt_started_at",
                table: "incident_notifications",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_error_code",
                table: "incident_notifications",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "notification_id",
                table: "incident_notifications",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "payload_fingerprint",
                table: "incident_notifications",
                type: "char(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                table: "incident_notifications",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_address",
                table: "incident_notifications",
                type: "varchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "requested_by_user_id",
                table: "incident_notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "incident_notifications",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "field_guides",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Data written by the first version of Incident Response and Expedition Setup.
            // 1. The in_response status becomes confirmed, its equivalent in the explicit lifecycle.
            migrationBuilder.Sql(
                "UPDATE incidents SET status = 'confirmed' WHERE status IN ('inresponse', 'in_response');");
            // 2. Every incident gets the expedition group of its tracking session, used for the group scope.
            migrationBuilder.Sql(
                "UPDATE incidents i JOIN tracking_sessions s ON s.id = i.session_id " +
                "SET i.expedition_group_id = s.expedition_group_id WHERE i.expedition_group_id = 0;");
            // 3. Notifications of the first version were marked as sent without contacting any provider: they get a
            //    stable identity and are kept as failed with an explicit code, never as delivered.
            migrationBuilder.Sql(
                "UPDATE incident_notifications SET notification_id = UUID() " +
                "WHERE notification_id = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql(
                "UPDATE incident_notifications SET status = 'failed', failure_code = 'legacy_unverified', " +
                "failed_at = requested_at WHERE status = 'sent';");
            // 4. Existing field guides receive a unique operational code derived from their identifier.
            // LPAD truncates when the value is longer than the requested length, so guides 100 and 1000 would both
            // become GU-100 and collide in ux_field_guides_agency_code. The padding only applies below three digits.
            migrationBuilder.Sql(
                "UPDATE field_guides SET code = CONCAT('GU-', IF(id < 1000, LPAD(id, 3, '0'), id)) WHERE code = '';");

            migrationBuilder.CreateTable(
                name: "evacuation_actions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    agency_id = table.Column<int>(type: "int", nullable: false),
                    incident_id = table.Column<int>(type: "int", nullable: false),
                    expedition_group_id = table.Column<int>(type: "int", nullable: false),
                    client_action_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    recorded_by_user_id = table.Column<int>(type: "int", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    synchronized_at = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    content_fingerprint = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_evacuation_actions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_evacuation_actions__incident_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incident_status_changes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    incident_id = table.Column<int>(type: "int", nullable: false),
                    from_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    actor_user_id = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_incident_status_changes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_incident_status_changes_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_users_agency_field_guide",
                table: "users",
                columns: new[] { "agency_id", "field_guide_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_incidents_agency_id_expedition_group_id",
                table: "incidents",
                columns: new[] { "agency_id", "expedition_group_id" });

            migrationBuilder.CreateIndex(
                name: "ux_incidents_agency_alert",
                table: "incidents",
                columns: new[] { "agency_id", "alert_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_incident_notifications_agency_notification",
                table: "incident_notifications",
                columns: new[] { "agency_id", "notification_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_field_guides_agency_code",
                table: "field_guides",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_evacuation_actions_incident_id",
                table: "evacuation_actions",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ux_evacuation_actions_agency_client_action",
                table: "evacuation_actions",
                columns: new[] { "agency_id", "client_action_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_incident_status_changes_incident_id",
                table: "incident_status_changes",
                column: "incident_id");

            // The new unique indexes start with agency_id, so they also back the agency foreign keys. MySQL refuses to
            // drop an index that backs a foreign key before its replacement exists.
            migrationBuilder.DropIndex(
                name: "i_x_users_agency_id_field_guide_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "i_x_field_guides_agency_id",
                table: "field_guides");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "i_x_users_agency_id_field_guide_id",
                table: "users",
                columns: new[] { "agency_id", "field_guide_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_field_guides_agency_id",
                table: "field_guides",
                column: "agency_id");

            migrationBuilder.Sql(
                "UPDATE incidents SET status = 'inresponse' WHERE status IN ('confirmed', 'emergency_declared');");
            migrationBuilder.Sql("UPDATE incidents SET status = 'closed' WHERE status = 'dismissed';");
            migrationBuilder.Sql(
                "UPDATE incident_notifications SET status = 'failed' WHERE status IN ('pending', 'accepted', 'delivered');");

            migrationBuilder.DropTable(
                name: "evacuation_actions");

            migrationBuilder.DropTable(
                name: "incident_status_changes");

            migrationBuilder.DropIndex(
                name: "ux_users_agency_field_guide",
                table: "users");

            migrationBuilder.DropIndex(
                name: "i_x_incidents_agency_id_expedition_group_id",
                table: "incidents");

            migrationBuilder.DropIndex(
                name: "ux_incidents_agency_alert",
                table: "incidents");

            migrationBuilder.DropIndex(
                name: "ux_incident_notifications_agency_notification",
                table: "incident_notifications");

            migrationBuilder.DropIndex(
                name: "ux_field_guides_agency_code",
                table: "field_guides");

            migrationBuilder.DropColumn(
                name: "health_notes",
                table: "participant_preparations");

            migrationBuilder.DropColumn(
                name: "health_notes_recorded_at",
                table: "participant_preparations");

            migrationBuilder.DropColumn(
                name: "alert_id",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "dismissed_at",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "emergency_declared_at",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "expedition_group_id",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "reported_by_user_id",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "body",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "delivered_at",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "failure_code",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "last_attempt_finished_at",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "last_attempt_started_at",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "last_error_code",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "notification_id",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "payload_fingerprint",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "recipient_address",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "requested_by_user_id",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "subject",
                table: "incident_notifications");

            migrationBuilder.DropColumn(
                name: "code",
                table: "field_guides");

            migrationBuilder.Sql(
                "ALTER TABLE incident_notifications RENAME COLUMN requested_at TO sent_at;");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "incidents",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

        }
    }
}
