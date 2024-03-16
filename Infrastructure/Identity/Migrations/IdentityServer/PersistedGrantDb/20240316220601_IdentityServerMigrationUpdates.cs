using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TB.DanceDance.Identity.Data.Migrations.IdentityServer.PersistedGrantDb
{
    /// <inheritdoc />
    public partial class IdentityServerMigrationUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PersistedGrants",
                schema: "IdpServer.Oper",
                table: "PersistedGrants");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "IdpServer.Oper",
                table: "PersistedGrants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<long>(
                name: "Id",
                schema: "IdpServer.Oper",
                table: "PersistedGrants",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersistedGrants",
                schema: "IdpServer.Oper",
                table: "PersistedGrants",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Keys",
                schema: "IdpServer.Oper",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Use = table.Column<string>(type: "text", nullable: true),
                    Algorithm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsX509Certificate = table.Column<bool>(type: "boolean", nullable: false),
                    DataProtected = table.Column<bool>(type: "boolean", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushedAuthorizationRequests",
                schema: "IdpServer.Oper",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceValueHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Parameters = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushedAuthorizationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerSideSessions",
                schema: "IdpServer.Oper",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Scheme = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubjectId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Renewed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSideSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersistedGrants_ConsumedTime",
                schema: "IdpServer.Oper",
                table: "PersistedGrants",
                column: "ConsumedTime");

            migrationBuilder.CreateIndex(
                name: "IX_PersistedGrants_Key",
                schema: "IdpServer.Oper",
                table: "PersistedGrants",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Keys_Use",
                schema: "IdpServer.Oper",
                table: "Keys",
                column: "Use");

            migrationBuilder.CreateIndex(
                name: "IX_PushedAuthorizationRequests_ExpiresAtUtc",
                schema: "IdpServer.Oper",
                table: "PushedAuthorizationRequests",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PushedAuthorizationRequests_ReferenceValueHash",
                schema: "IdpServer.Oper",
                table: "PushedAuthorizationRequests",
                column: "ReferenceValueHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerSideSessions_DisplayName",
                schema: "IdpServer.Oper",
                table: "ServerSideSessions",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_ServerSideSessions_Expires",
                schema: "IdpServer.Oper",
                table: "ServerSideSessions",
                column: "Expires");

            migrationBuilder.CreateIndex(
                name: "IX_ServerSideSessions_Key",
                schema: "IdpServer.Oper",
                table: "ServerSideSessions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerSideSessions_SessionId",
                schema: "IdpServer.Oper",
                table: "ServerSideSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerSideSessions_SubjectId",
                schema: "IdpServer.Oper",
                table: "ServerSideSessions",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Keys",
                schema: "IdpServer.Oper");

            migrationBuilder.DropTable(
                name: "PushedAuthorizationRequests",
                schema: "IdpServer.Oper");

            migrationBuilder.DropTable(
                name: "ServerSideSessions",
                schema: "IdpServer.Oper");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersistedGrants",
                schema: "IdpServer.Oper",
                table: "PersistedGrants");

            migrationBuilder.DropIndex(
                name: "IX_PersistedGrants_ConsumedTime",
                schema: "IdpServer.Oper",
                table: "PersistedGrants");

            migrationBuilder.DropIndex(
                name: "IX_PersistedGrants_Key",
                schema: "IdpServer.Oper",
                table: "PersistedGrants");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "IdpServer.Oper",
                table: "PersistedGrants");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                schema: "IdpServer.Oper",
                table: "PersistedGrants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersistedGrants",
                schema: "IdpServer.Oper",
                table: "PersistedGrants",
                column: "Key");
        }
    }
}
