using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "access");

        migrationBuilder.EnsureSchema(
            name: "video");

        migrationBuilder.CreateTable(
            name: "Events",
            schema: "access",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Groups",
            schema: "access",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Groups", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ToTransform",
            schema: "video",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BlobId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                UploadedBy = table.Column<string>(type: "text", nullable: false),
                RecordedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SharedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                SharedWithId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedToEvent = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ToTransform", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Videos",
            schema: "video",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BlobId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                UploadedBy = table.Column<string>(type: "text", nullable: false),
                RecordedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SharedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Duration = table.Column<TimeSpan>(type: "interval", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Videos", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AssingedToEvents",
            schema: "access",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssingedToEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_AssingedToEvents_Events_EventId",
                    column: x => x.EventId,
                    principalSchema: "access",
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EventAssigmentRequests",
            schema: "access",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventAssigmentRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_EventAssigmentRequests_Events_EventId",
                    column: x => x.EventId,
                    principalSchema: "access",
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AssingedToGroups",
            schema: "access",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssingedToGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_AssingedToGroups_Groups_GroupId",
                    column: x => x.GroupId,
                    principalSchema: "access",
                    principalTable: "Groups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GroupAssigmentRequests",
            schema: "access",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                GroupId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupAssigmentRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_GroupAssigmentRequests_Groups_GroupId",
                    column: x => x.GroupId,
                    principalSchema: "access",
                    principalTable: "Groups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SharedWith",
            schema: "access",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: true),
                GroupId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharedWith", x => x.Id);
                table.ForeignKey(
                    name: "FK_SharedWith_Events_EventId",
                    column: x => x.EventId,
                    principalSchema: "access",
                    principalTable: "Events",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_SharedWith_Groups_GroupId",
                    column: x => x.GroupId,
                    principalSchema: "access",
                    principalTable: "Groups",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_SharedWith_Videos_VideoId",
                    column: x => x.VideoId,
                    principalSchema: "video",
                    principalTable: "Videos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VideoMetadata",
            schema: "video",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                Metadata = table.Column<byte[]>(type: "bytea", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VideoMetadata", x => x.Id);
                table.ForeignKey(
                    name: "FK_VideoMetadata_Videos_VideoId",
                    column: x => x.VideoId,
                    principalSchema: "video",
                    principalTable: "Videos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AssingedToEvents_EventId",
            schema: "access",
            table: "AssingedToEvents",
            column: "EventId");

        migrationBuilder.CreateIndex(
            name: "IX_AssingedToGroups_GroupId",
            schema: "access",
            table: "AssingedToGroups",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_EventAssigmentRequests_EventId",
            schema: "access",
            table: "EventAssigmentRequests",
            column: "EventId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupAssigmentRequests_GroupId",
            schema: "access",
            table: "GroupAssigmentRequests",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedWith_EventId",
            schema: "access",
            table: "SharedWith",
            column: "EventId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedWith_GroupId",
            schema: "access",
            table: "SharedWith",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_SharedWith_VideoId",
            schema: "access",
            table: "SharedWith",
            column: "VideoId");

        migrationBuilder.CreateIndex(
            name: "IX_VideoMetadata_VideoId",
            schema: "video",
            table: "VideoMetadata",
            column: "VideoId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AssingedToEvents",
            schema: "access");

        migrationBuilder.DropTable(
            name: "AssingedToGroups",
            schema: "access");

        migrationBuilder.DropTable(
            name: "EventAssigmentRequests",
            schema: "access");

        migrationBuilder.DropTable(
            name: "GroupAssigmentRequests",
            schema: "access");

        migrationBuilder.DropTable(
            name: "SharedWith",
            schema: "access");

        migrationBuilder.DropTable(
            name: "ToTransform",
            schema: "video");

        migrationBuilder.DropTable(
            name: "VideoMetadata",
            schema: "video");

        migrationBuilder.DropTable(
            name: "Events",
            schema: "access");

        migrationBuilder.DropTable(
            name: "Groups",
            schema: "access");

        migrationBuilder.DropTable(
            name: "Videos",
            schema: "video");
    }
}
