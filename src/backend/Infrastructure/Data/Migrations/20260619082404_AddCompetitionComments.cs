using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "VideoId",
                schema: "comments",
                table: "Comments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CompetitionId",
                schema: "comments",
                table: "Comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CompetitionId",
                schema: "comments",
                table: "Comments",
                column: "CompetitionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_VideoOrCompetition",
                schema: "comments",
                table: "Comments",
                sql: "(\"VideoId\" IS NOT NULL) <> (\"CompetitionId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Competitions_CompetitionId",
                schema: "comments",
                table: "Comments",
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
                name: "FK_Comments_Competitions_CompetitionId",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_CompetitionId",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_VideoOrCompetition",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CompetitionId",
                schema: "comments",
                table: "Comments");

            migrationBuilder.AlterColumn<Guid>(
                name: "VideoId",
                schema: "comments",
                table: "Comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
