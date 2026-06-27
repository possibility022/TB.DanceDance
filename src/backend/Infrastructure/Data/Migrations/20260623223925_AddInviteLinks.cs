using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InviteLinks",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RedeemedByUserId = table.Column<string>(type: "text", nullable: true),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteLinks", x => x.Id);
                    table.CheckConstraint("CK_InviteLinks_GroupOrEvent", "(\"GroupId\" IS NOT NULL) <> (\"EventId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_InviteLinks_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "access",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InviteLinks_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "access",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InviteLinks_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InviteLinks_Users_RedeemedByUserId",
                        column: x => x.RedeemedByUserId,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InviteLinks_CreatedBy",
                schema: "access",
                table: "InviteLinks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InviteLinks_EventId",
                schema: "access",
                table: "InviteLinks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteLinks_GroupId",
                schema: "access",
                table: "InviteLinks",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteLinks_RedeemedByUserId",
                schema: "access",
                table: "InviteLinks",
                column: "RedeemedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InviteLinks",
                schema: "access");
        }
    }
}
