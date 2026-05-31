using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Access.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "access");

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SeasonStart = table.Column<DateOnly>(type: "date", nullable: false),
                    SeasonEnd = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    StorageQuotaBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignedToGroups",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    WhenJoined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedToGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignedToGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "access",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignedToGroups_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Users_Owner",
                        column: x => x.Owner,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupAssignmentRequests",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    WhenJoined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: true),
                    ManagedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupAssignmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupAssignmentRequests_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "access",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupAssignmentRequests_Users_ManagedBy",
                        column: x => x.ManagedBy,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GroupsAdmins",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupsAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupsAdmins_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "access",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupsAdmins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignedToEvents",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedToEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignedToEvents_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "access",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignedToEvents_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAssignmentRequests",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: true),
                    ManagedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAssignmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAssignmentRequests_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "access",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAssignmentRequests_Users_ManagedBy",
                        column: x => x.ManagedBy,
                        principalSchema: "access",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignedToEvents_EventId",
                schema: "access",
                table: "AssignedToEvents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedToEvents_UserId",
                schema: "access",
                table: "AssignedToEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedToGroups_GroupId",
                schema: "access",
                table: "AssignedToGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedToGroups_UserId",
                schema: "access",
                table: "AssignedToGroups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAssignmentRequests_EventId",
                schema: "access",
                table: "EventAssignmentRequests",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAssignmentRequests_ManagedBy",
                schema: "access",
                table: "EventAssignmentRequests",
                column: "ManagedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Owner",
                schema: "access",
                table: "Events",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAssignmentRequests_GroupId",
                schema: "access",
                table: "GroupAssignmentRequests",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAssignmentRequests_ManagedBy",
                schema: "access",
                table: "GroupAssignmentRequests",
                column: "ManagedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GroupsAdmins_GroupId",
                schema: "access",
                table: "GroupsAdmins",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupsAdmins_UserId",
                schema: "access",
                table: "GroupsAdmins",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignedToEvents",
                schema: "access");

            migrationBuilder.DropTable(
                name: "AssignedToGroups",
                schema: "access");

            migrationBuilder.DropTable(
                name: "EventAssignmentRequests",
                schema: "access");

            migrationBuilder.DropTable(
                name: "GroupAssignmentRequests",
                schema: "access");

            migrationBuilder.DropTable(
                name: "GroupsAdmins",
                schema: "access");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "access");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "access");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "access");
        }
    }
}
