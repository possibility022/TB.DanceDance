using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SharedLinksFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedLinks",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedLinks_Users_SharedBy",
                        column: x => x.SharedBy,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedLinks_Videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "video",
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_Id",
                schema: "access",
                table: "SharedLinks",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_SharedBy",
                schema: "access",
                table: "SharedLinks",
                column: "SharedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_VideoId",
                schema: "access",
                table: "SharedLinks",
                column: "VideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedLinks",
                schema: "access");
        }
    }
}
