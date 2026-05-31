using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Videos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "comments");

            migrationBuilder.EnsureSchema(
                name: "video");

            migrationBuilder.CreateTable(
                name: "Videos",
                schema: "video",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: false),
                    RecordedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SharedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    LockedTill = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceBlobId = table.Column<string>(type: "text", nullable: false),
                    Converted = table.Column<bool>(type: "boolean", nullable: false),
                    SourceBlobSize = table.Column<long>(type: "bigint", nullable: false),
                    ConvertedBlobSize = table.Column<long>(type: "bigint", nullable: false),
                    CommentVisibility = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SharedLinks",
                schema: "video",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    AllowComments = table.Column<bool>(type: "boolean", nullable: false),
                    AllowAnonymousComments = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedLinks_Videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "video",
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedWith",
                schema: "video",
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

            migrationBuilder.CreateTable(
                name: "Comments",
                schema: "comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    SharedLinkId = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PostedAsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    AnonymousName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ShaOfAnonymousId = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    IsReported = table.Column<bool>(type: "boolean", nullable: false),
                    ReportedReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_SharedLinks_SharedLinkId",
                        column: x => x.SharedLinkId,
                        principalSchema: "video",
                        principalTable: "SharedLinks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_Videos_VideoId",
                        column: x => x.VideoId,
                        principalSchema: "video",
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_SharedLinkId",
                schema: "comments",
                table: "Comments",
                column: "SharedLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                schema: "comments",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_VideoId",
                schema: "comments",
                table: "Comments",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_SharedBy",
                schema: "video",
                table: "SharedLinks",
                column: "SharedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_VideoId",
                schema: "video",
                table: "SharedLinks",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedWith_EventId",
                schema: "video",
                table: "SharedWith",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedWith_GroupId",
                schema: "video",
                table: "SharedWith",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedWith_UserId",
                schema: "video",
                table: "SharedWith",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedWith_VideoId",
                schema: "video",
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
                name: "Comments",
                schema: "comments");

            migrationBuilder.DropTable(
                name: "SharedWith",
                schema: "video");

            migrationBuilder.DropTable(
                name: "VideoMetadata",
                schema: "video");

            migrationBuilder.DropTable(
                name: "SharedLinks",
                schema: "video");

            migrationBuilder.DropTable(
                name: "Videos",
                schema: "video");
        }
    }
}
