using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class MoreInformationsAboutAssigmentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                schema: "access",
                table: "GroupAssigmentRequests",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagedBy",
                schema: "access",
                table: "GroupAssigmentRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                schema: "access",
                table: "EventAssigmentRequests",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagedBy",
                schema: "access",
                table: "EventAssigmentRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupAssigmentRequests_ManagedBy",
                schema: "access",
                table: "GroupAssigmentRequests",
                column: "ManagedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAssigmentRequests_ManagedBy",
                schema: "access",
                table: "EventAssigmentRequests",
                column: "ManagedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_EventAssigmentRequests_Users_ManagedBy",
                schema: "access",
                table: "EventAssigmentRequests",
                column: "ManagedBy",
                principalSchema: "access",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupAssigmentRequests_Users_ManagedBy",
                schema: "access",
                table: "GroupAssigmentRequests",
                column: "ManagedBy",
                principalSchema: "access",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventAssigmentRequests_Users_ManagedBy",
                schema: "access",
                table: "EventAssigmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupAssigmentRequests_Users_ManagedBy",
                schema: "access",
                table: "GroupAssigmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_GroupAssigmentRequests_ManagedBy",
                schema: "access",
                table: "GroupAssigmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_EventAssigmentRequests_ManagedBy",
                schema: "access",
                table: "EventAssigmentRequests");

            migrationBuilder.DropColumn(
                name: "Approved",
                schema: "access",
                table: "GroupAssigmentRequests");

            migrationBuilder.DropColumn(
                name: "ManagedBy",
                schema: "access",
                table: "GroupAssigmentRequests");

            migrationBuilder.DropColumn(
                name: "Approved",
                schema: "access",
                table: "EventAssigmentRequests");

            migrationBuilder.DropColumn(
                name: "ManagedBy",
                schema: "access",
                table: "EventAssigmentRequests");
        }
    }
}
