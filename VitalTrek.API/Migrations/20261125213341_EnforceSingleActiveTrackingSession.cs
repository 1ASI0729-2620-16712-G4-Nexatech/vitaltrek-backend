using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitalTrek.API.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveTrackingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One atomic MySQL 8 ALTER: existing duplicates must leave both schema and data unchanged.
            migrationBuilder.Sql("""
                ALTER TABLE `tracking_sessions`
                ADD COLUMN `active_expedition_group_id` int GENERATED ALWAYS AS
                    (CASE WHEN `status` = 'active' THEN `expedition_group_id` ELSE NULL END) STORED,
                ADD UNIQUE INDEX `ux_tracking_sessions_active_group` (`active_expedition_group_id`);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `tracking_sessions`
                DROP INDEX `ux_tracking_sessions_active_group`,
                DROP COLUMN `active_expedition_group_id`;
                """);
        }
    }
}
