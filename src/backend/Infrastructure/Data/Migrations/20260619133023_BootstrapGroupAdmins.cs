using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BootstrapGroupAdmins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration: make every existing group member an admin of that group,
            // so groups created before admin management have at least one admin. Idempotent
            // (NOT EXISTS), so re-running is a no-op. SQL shared with GroupAdminBootstrap.Sql.
            migrationBuilder.Sql(GroupAdminBootstrap.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible by design: bootstrapped admin rows are indistinguishable from
            // admins granted later, so there is nothing safe to undo. No-op.
        }
    }
}
