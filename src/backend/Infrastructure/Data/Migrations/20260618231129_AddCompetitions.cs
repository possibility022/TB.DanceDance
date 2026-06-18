using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompetitionId",
                schema: "video",
                table: "Videos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Competitions",
                schema: "video",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OwnerUserId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    CommentVisibility = table.Column<int>(type: "integer", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Videos_CompetitionId",
                schema: "video",
                table: "Videos",
                column: "CompetitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_Competitions_CompetitionId",
                schema: "video",
                table: "Videos",
                column: "CompetitionId",
                principalSchema: "video",
                principalTable: "Competitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Videos_Competitions_CompetitionId",
                schema: "video",
                table: "Videos");

            migrationBuilder.DropTable(
                name: "Competitions",
                schema: "video");

            migrationBuilder.DropIndex(
                name: "IX_Videos_CompetitionId",
                schema: "video",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "CompetitionId",
                schema: "video",
                table: "Videos");
        }
    }
}
