using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TB.DanceDance.Identity.Data.Migrations.IdentityServer.ConfigurationDb
{
    /// <inheritdoc />
    public partial class IdentityServerMigrationUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentityResourceProperties_IdentityResourceId",
                schema: "IdpServer.Config",
                table: "IdentityResourceProperties");

            migrationBuilder.DropIndex(
                name: "IX_IdentityResourceClaims_IdentityResourceId",
                schema: "IdpServer.Config",
                table: "IdentityResourceClaims");

            migrationBuilder.DropIndex(
                name: "IX_ClientScopes_ClientId",
                schema: "IdpServer.Config",
                table: "ClientScopes");

            migrationBuilder.DropIndex(
                name: "IX_ClientRedirectUris_ClientId",
                schema: "IdpServer.Config",
                table: "ClientRedirectUris");

            migrationBuilder.DropIndex(
                name: "IX_ClientProperties_ClientId",
                schema: "IdpServer.Config",
                table: "ClientProperties");

            migrationBuilder.DropIndex(
                name: "IX_ClientPostLogoutRedirectUris_ClientId",
                schema: "IdpServer.Config",
                table: "ClientPostLogoutRedirectUris");

            migrationBuilder.DropIndex(
                name: "IX_ClientIdPRestrictions_ClientId",
                schema: "IdpServer.Config",
                table: "ClientIdPRestrictions");

            migrationBuilder.DropIndex(
                name: "IX_ClientGrantTypes_ClientId",
                schema: "IdpServer.Config",
                table: "ClientGrantTypes");

            migrationBuilder.DropIndex(
                name: "IX_ClientCorsOrigins_ClientId",
                schema: "IdpServer.Config",
                table: "ClientCorsOrigins");

            migrationBuilder.DropIndex(
                name: "IX_ClientClaims_ClientId",
                schema: "IdpServer.Config",
                table: "ClientClaims");

            migrationBuilder.DropIndex(
                name: "IX_ApiScopeProperties_ScopeId",
                schema: "IdpServer.Config",
                table: "ApiScopeProperties");

            migrationBuilder.DropIndex(
                name: "IX_ApiScopeClaims_ScopeId",
                schema: "IdpServer.Config",
                table: "ApiScopeClaims");

            migrationBuilder.DropIndex(
                name: "IX_ApiResourceScopes_ApiResourceId",
                schema: "IdpServer.Config",
                table: "ApiResourceScopes");

            migrationBuilder.DropIndex(
                name: "IX_ApiResourceProperties_ApiResourceId",
                schema: "IdpServer.Config",
                table: "ApiResourceProperties");

            migrationBuilder.DropIndex(
                name: "IX_ApiResourceClaims_ApiResourceId",
                schema: "IdpServer.Config",
                table: "ApiResourceClaims");

            migrationBuilder.AddColumn<int>(
                name: "CibaLifetime",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CoordinateLifetimeWithUserSession",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DPoPClockSkew",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "DPoPValidationMode",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InitiateLoginUri",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PollingInterval",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PushedAuthorizationLifetime",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireDPoP",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePushedAuthorization",
                schema: "IdpServer.Config",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "RedirectUri",
                schema: "IdpServer.Config",
                table: "ClientRedirectUris",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "PostLogoutRedirectUri",
                schema: "IdpServer.Config",
                table: "ClientPostLogoutRedirectUris",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                schema: "IdpServer.Config",
                table: "ApiScopes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessed",
                schema: "IdpServer.Config",
                table: "ApiScopes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NonEditable",
                schema: "IdpServer.Config",
                table: "ApiScopes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "Updated",
                schema: "IdpServer.Config",
                table: "ApiScopes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireResourceIndicator",
                schema: "IdpServer.Config",
                table: "ApiResources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "IdentityProviders",
                schema: "IdpServer.Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Scheme = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NonEditable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityResourceProperties_IdentityResourceId_Key",
                schema: "IdpServer.Config",
                table: "IdentityResourceProperties",
                columns: new[] { "IdentityResourceId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityResourceClaims_IdentityResourceId_Type",
                schema: "IdpServer.Config",
                table: "IdentityResourceClaims",
                columns: new[] { "IdentityResourceId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientScopes_ClientId_Scope",
                schema: "IdpServer.Config",
                table: "ClientScopes",
                columns: new[] { "ClientId", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientRedirectUris_ClientId_RedirectUri",
                schema: "IdpServer.Config",
                table: "ClientRedirectUris",
                columns: new[] { "ClientId", "RedirectUri" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientProperties_ClientId_Key",
                schema: "IdpServer.Config",
                table: "ClientProperties",
                columns: new[] { "ClientId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientPostLogoutRedirectUris_ClientId_PostLogoutRedirectUri",
                schema: "IdpServer.Config",
                table: "ClientPostLogoutRedirectUris",
                columns: new[] { "ClientId", "PostLogoutRedirectUri" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientIdPRestrictions_ClientId_Provider",
                schema: "IdpServer.Config",
                table: "ClientIdPRestrictions",
                columns: new[] { "ClientId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientGrantTypes_ClientId_GrantType",
                schema: "IdpServer.Config",
                table: "ClientGrantTypes",
                columns: new[] { "ClientId", "GrantType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientCorsOrigins_ClientId_Origin",
                schema: "IdpServer.Config",
                table: "ClientCorsOrigins",
                columns: new[] { "ClientId", "Origin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientClaims_ClientId_Type_Value",
                schema: "IdpServer.Config",
                table: "ClientClaims",
                columns: new[] { "ClientId", "Type", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiScopeProperties_ScopeId_Key",
                schema: "IdpServer.Config",
                table: "ApiScopeProperties",
                columns: new[] { "ScopeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiScopeClaims_ScopeId_Type",
                schema: "IdpServer.Config",
                table: "ApiScopeClaims",
                columns: new[] { "ScopeId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiResourceScopes_ApiResourceId_Scope",
                schema: "IdpServer.Config",
                table: "ApiResourceScopes",
                columns: new[] { "ApiResourceId", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiResourceProperties_ApiResourceId_Key",
                schema: "IdpServer.Config",
                table: "ApiResourceProperties",
                columns: new[] { "ApiResourceId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiResourceClaims_ApiResourceId_Type",
                schema: "IdpServer.Config",
                table: "ApiResourceClaims",
                columns: new[] { "ApiResourceId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviders_Scheme",
                schema: "IdpServer.Config",
                table: "IdentityProviders",
                column: "Scheme",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityProviders",
                schema: "IdpServer.Config");

            migrationBuilder.DropIndex(
                name: "IX_IdentityResourceProperties_IdentityResourceId_Key",
                schema: "IdpServer.Config",
                table: "IdentityResourceProperties");

            migrationBuilder.DropIndex(
                name: "IX_IdentityResourceClaims_IdentityResourceId_Type",
                schema: "IdpServer.Config",
                table: "IdentityResourceClaims");

            migrationBuilder.DropIndex(
                name: "IX_ClientScopes_ClientId_Scope",
                schema: "IdpServer.Config",
                table: "ClientScopes");

            migrationBuilder.DropIndex(
                name: "IX_ClientRedirectUris_ClientId_RedirectUri",
                schema: "IdpServer.Config",
                table: "ClientRedirectUris");

            migrationBuilder.DropIndex(
                name: "IX_ClientProperties_ClientId_Key",
                schema: "IdpServer.Config",
                table: "ClientProperties");

            migrationBuilder.DropIndex(
                name: "IX_ClientPostLogoutRedirectUris_ClientId_PostLogoutRedirectUri",
                schema: "IdpServer.Config",
                table: "ClientPostLogoutRedirectUris");

            migrationBuilder.DropIndex(
                name: "IX_ClientIdPRestrictions_ClientId_Provider",
                schema: "IdpServer.Config",
                table: "ClientIdPRestrictions");

            migrationBuilder.DropIndex(
                name: "IX_ClientGrantTypes_ClientId_GrantType",
                schema: "IdpServer.Config",
                table: "ClientGrantTypes");

            migrationBuilder.DropIndex(
                name: "IX_ClientCorsOrigins_ClientId_Origin",
                schema: "IdpServer.Config",
                table: "ClientCorsOrigins");

            migrationBuilder.DropIndex(
                name: "IX_ClientClaims_ClientId_Type_Value",
                schema: "IdpServer.Config",
                table: "ClientClaims");

            migrationBuilder.DropIndex(
                name: "IX_ApiScopeProperties_ScopeId_Key",
                schema: "IdpServer.Config",
                table: "ApiScopeProperties");

            migrationBuilder.DropIndex(
                name: "IX_ApiScopeClaims_ScopeId_Type",
                schema: "IdpServer.Config",
                table: "ApiScopeClaims");

            migrationBuilder.DropIndex(
                name: "IX_ApiResourceScopes_ApiResourceId_Scope",
                schema: "IdpServer.Config",
                table: "ApiResourceScopes");

            migrationBuilder.DropIndex(
                name: "IX_ApiResourceProperties_ApiResourceId_Key",
                schema: "IdpServer.Config",
                table: "ApiResourceProperties");

            migrationBuilder.DropIndex(
                name: "IX_ApiResourceClaims_ApiResourceId_Type",
                schema: "IdpServer.Config",
                table: "ApiResourceClaims");

            migrationBuilder.DropColumn(
                name: "CibaLifetime",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CoordinateLifetimeWithUserSession",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DPoPClockSkew",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DPoPValidationMode",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "InitiateLoginUri",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PollingInterval",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PushedAuthorizationLifetime",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RequireDPoP",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RequirePushedAuthorization",
                schema: "IdpServer.Config",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Created",
                schema: "IdpServer.Config",
                table: "ApiScopes");

            migrationBuilder.DropColumn(
                name: "LastAccessed",
                schema: "IdpServer.Config",
                table: "ApiScopes");

            migrationBuilder.DropColumn(
                name: "NonEditable",
                schema: "IdpServer.Config",
                table: "ApiScopes");

            migrationBuilder.DropColumn(
                name: "Updated",
                schema: "IdpServer.Config",
                table: "ApiScopes");

            migrationBuilder.DropColumn(
                name: "RequireResourceIndicator",
                schema: "IdpServer.Config",
                table: "ApiResources");

            migrationBuilder.AlterColumn<string>(
                name: "RedirectUri",
                schema: "IdpServer.Config",
                table: "ClientRedirectUris",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "PostLogoutRedirectUri",
                schema: "IdpServer.Config",
                table: "ClientPostLogoutRedirectUris",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityResourceProperties_IdentityResourceId",
                schema: "IdpServer.Config",
                table: "IdentityResourceProperties",
                column: "IdentityResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityResourceClaims_IdentityResourceId",
                schema: "IdpServer.Config",
                table: "IdentityResourceClaims",
                column: "IdentityResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientScopes_ClientId",
                schema: "IdpServer.Config",
                table: "ClientScopes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientRedirectUris_ClientId",
                schema: "IdpServer.Config",
                table: "ClientRedirectUris",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProperties_ClientId",
                schema: "IdpServer.Config",
                table: "ClientProperties",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPostLogoutRedirectUris_ClientId",
                schema: "IdpServer.Config",
                table: "ClientPostLogoutRedirectUris",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIdPRestrictions_ClientId",
                schema: "IdpServer.Config",
                table: "ClientIdPRestrictions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGrantTypes_ClientId",
                schema: "IdpServer.Config",
                table: "ClientGrantTypes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCorsOrigins_ClientId",
                schema: "IdpServer.Config",
                table: "ClientCorsOrigins",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientClaims_ClientId",
                schema: "IdpServer.Config",
                table: "ClientClaims",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiScopeProperties_ScopeId",
                schema: "IdpServer.Config",
                table: "ApiScopeProperties",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiScopeClaims_ScopeId",
                schema: "IdpServer.Config",
                table: "ApiScopeClaims",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiResourceScopes_ApiResourceId",
                schema: "IdpServer.Config",
                table: "ApiResourceScopes",
                column: "ApiResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiResourceProperties_ApiResourceId",
                schema: "IdpServer.Config",
                table: "ApiResourceProperties",
                column: "ApiResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiResourceClaims_ApiResourceId",
                schema: "IdpServer.Config",
                table: "ApiResourceClaims",
                column: "ApiResourceId");
        }
    }
}
