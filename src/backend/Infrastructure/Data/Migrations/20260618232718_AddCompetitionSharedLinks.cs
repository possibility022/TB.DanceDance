using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionSharedLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "VideoId",
                schema: "access",
                table: "SharedLinks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CompetitionId",
                schema: "access",
                table: "SharedLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_CompetitionId",
                schema: "access",
                table: "SharedLinks",
                column: "CompetitionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SharedLinks_VideoOrCompetition",
                schema: "access",
                table: "SharedLinks",
                sql: "(\"VideoId\" IS NOT NULL) <> (\"CompetitionId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedLinks_Competitions_CompetitionId",
                schema: "access",
                table: "SharedLinks",
                column: "CompetitionId",
                principalSchema: "video",
                principalTable: "Competitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedLinks_Competitions_CompetitionId",
                schema: "access",
                table: "SharedLinks");

            migrationBuilder.DropIndex(
                name: "IX_SharedLinks_CompetitionId",
                schema: "access",
                table: "SharedLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SharedLinks_VideoOrCompetition",
                schema: "access",
                table: "SharedLinks");

            migrationBuilder.DropColumn(
                name: "CompetitionId",
                schema: "access",
                table: "SharedLinks");

            migrationBuilder.AlterColumn<Guid>(
                name: "VideoId",
                schema: "access",
                table: "SharedLinks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
