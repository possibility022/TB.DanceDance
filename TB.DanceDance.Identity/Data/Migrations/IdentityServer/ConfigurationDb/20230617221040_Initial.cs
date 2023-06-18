using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TB.DanceDance.Identity.Data.Migrations.IdentityServer.ConfigurationDb;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "IdpServer.Config");

        migrationBuilder.CreateTable(
            name: "ApiResources",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                AllowedAccessTokenSigningAlgorithms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ShowInDiscoveryDocument = table.Column<bool>(type: "boolean", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastAccessed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NonEditable = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiResources", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ApiScopes",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Required = table.Column<bool>(type: "boolean", nullable: false),
                Emphasize = table.Column<bool>(type: "boolean", nullable: false),
                ShowInDiscoveryDocument = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiScopes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Clients",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ProtocolType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                RequireClientSecret = table.Column<bool>(type: "boolean", nullable: false),
                ClientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ClientUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                LogoUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                RequireConsent = table.Column<bool>(type: "boolean", nullable: false),
                AllowRememberConsent = table.Column<bool>(type: "boolean", nullable: false),
                AlwaysIncludeUserClaimsInIdToken = table.Column<bool>(type: "boolean", nullable: false),
                RequirePkce = table.Column<bool>(type: "boolean", nullable: false),
                AllowPlainTextPkce = table.Column<bool>(type: "boolean", nullable: false),
                RequireRequestObject = table.Column<bool>(type: "boolean", nullable: false),
                AllowAccessTokensViaBrowser = table.Column<bool>(type: "boolean", nullable: false),
                FrontChannelLogoutUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                FrontChannelLogoutSessionRequired = table.Column<bool>(type: "boolean", nullable: false),
                BackChannelLogoutUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                BackChannelLogoutSessionRequired = table.Column<bool>(type: "boolean", nullable: false),
                AllowOfflineAccess = table.Column<bool>(type: "boolean", nullable: false),
                IdentityTokenLifetime = table.Column<int>(type: "integer", nullable: false),
                AllowedIdentityTokenSigningAlgorithms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                AccessTokenLifetime = table.Column<int>(type: "integer", nullable: false),
                AuthorizationCodeLifetime = table.Column<int>(type: "integer", nullable: false),
                ConsentLifetime = table.Column<int>(type: "integer", nullable: true),
                AbsoluteRefreshTokenLifetime = table.Column<int>(type: "integer", nullable: false),
                SlidingRefreshTokenLifetime = table.Column<int>(type: "integer", nullable: false),
                RefreshTokenUsage = table.Column<int>(type: "integer", nullable: false),
                UpdateAccessTokenClaimsOnRefresh = table.Column<bool>(type: "boolean", nullable: false),
                RefreshTokenExpiration = table.Column<int>(type: "integer", nullable: false),
                AccessTokenType = table.Column<int>(type: "integer", nullable: false),
                EnableLocalLogin = table.Column<bool>(type: "boolean", nullable: false),
                IncludeJwtId = table.Column<bool>(type: "boolean", nullable: false),
                AlwaysSendClientClaims = table.Column<bool>(type: "boolean", nullable: false),
                ClientClaimsPrefix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                PairWiseSubjectSalt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastAccessed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UserSsoLifetime = table.Column<int>(type: "integer", nullable: true),
                UserCodeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                DeviceCodeLifetime = table.Column<int>(type: "integer", nullable: false),
                NonEditable = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "IdentityResources",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Required = table.Column<bool>(type: "boolean", nullable: false),
                Emphasize = table.Column<bool>(type: "boolean", nullable: false),
                ShowInDiscoveryDocument = table.Column<bool>(type: "boolean", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NonEditable = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityResources", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ApiResourceClaims",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApiResourceId = table.Column<int>(type: "integer", nullable: false),
                Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiResourceClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiResourceClaims_ApiResources_ApiResourceId",
                    column: x => x.ApiResourceId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "ApiResources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ApiResourceProperties",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApiResourceId = table.Column<int>(type: "integer", nullable: false),
                Key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiResourceProperties", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiResourceProperties_ApiResources_ApiResourceId",
                    column: x => x.ApiResourceId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "ApiResources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ApiResourceScopes",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ApiResourceId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiResourceScopes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiResourceScopes_ApiResources_ApiResourceId",
                    column: x => x.ApiResourceId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "ApiResources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ApiResourceSecrets",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApiResourceId = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiResourceSecrets", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiResourceSecrets_ApiResources_ApiResourceId",
                    column: x => x.ApiResourceId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "ApiResources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ApiScopeClaims",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ScopeId = table.Column<int>(type: "integer", nullable: false),
                Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiScopeClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiScopeClaims_ApiScopes_ScopeId",
                    column: x => x.ScopeId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "ApiScopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ApiScopeProperties",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ScopeId = table.Column<int>(type: "integer", nullable: false),
                Key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiScopeProperties", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiScopeProperties_ApiScopes_ScopeId",
                    column: x => x.ScopeId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "ApiScopes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientClaims",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Value = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                ClientId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientClaims_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientCorsOrigins",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Origin = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                ClientId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientCorsOrigins", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientCorsOrigins_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientGrantTypes",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                GrantType = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                ClientId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientGrantTypes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientGrantTypes_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientIdPRestrictions",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ClientId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientIdPRestrictions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientIdPRestrictions_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientPostLogoutRedirectUris",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PostLogoutRedirectUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ClientId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientPostLogoutRedirectUris", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientPostLogoutRedirectUris_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientProperties",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ClientId = table.Column<int>(type: "integer", nullable: false),
                Key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientProperties", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientProperties_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientRedirectUris",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RedirectUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ClientId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientRedirectUris", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientRedirectUris_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientScopes",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ClientId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientScopes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientScopes_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ClientSecrets",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ClientId = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientSecrets", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientSecrets_Clients_ClientId",
                    column: x => x.ClientId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "IdentityResourceClaims",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IdentityResourceId = table.Column<int>(type: "integer", nullable: false),
                Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityResourceClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_IdentityResourceClaims_IdentityResources_IdentityResourceId",
                    column: x => x.IdentityResourceId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "IdentityResources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "IdentityResourceProperties",
            schema: "IdpServer.Config",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                IdentityResourceId = table.Column<int>(type: "integer", nullable: false),
                Key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityResourceProperties", x => x.Id);
                table.ForeignKey(
                    name: "FK_IdentityResourceProperties_IdentityResources_IdentityResour~",
                    column: x => x.IdentityResourceId,
                    principalSchema: "IdpServer.Config",
                    principalTable: "IdentityResources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ApiResourceClaims_ApiResourceId",
            schema: "IdpServer.Config",
            table: "ApiResourceClaims",
            column: "ApiResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiResourceProperties_ApiResourceId",
            schema: "IdpServer.Config",
            table: "ApiResourceProperties",
            column: "ApiResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiResources_Name",
            schema: "IdpServer.Config",
            table: "ApiResources",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ApiResourceScopes_ApiResourceId",
            schema: "IdpServer.Config",
            table: "ApiResourceScopes",
            column: "ApiResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiResourceSecrets_ApiResourceId",
            schema: "IdpServer.Config",
            table: "ApiResourceSecrets",
            column: "ApiResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiScopeClaims_ScopeId",
            schema: "IdpServer.Config",
            table: "ApiScopeClaims",
            column: "ScopeId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiScopeProperties_ScopeId",
            schema: "IdpServer.Config",
            table: "ApiScopeProperties",
            column: "ScopeId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiScopes_Name",
            schema: "IdpServer.Config",
            table: "ApiScopes",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClientClaims_ClientId",
            schema: "IdpServer.Config",
            table: "ClientClaims",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClientCorsOrigins_ClientId",
            schema: "IdpServer.Config",
            table: "ClientCorsOrigins",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClientGrantTypes_ClientId",
            schema: "IdpServer.Config",
            table: "ClientGrantTypes",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClientIdPRestrictions_ClientId",
            schema: "IdpServer.Config",
            table: "ClientIdPRestrictions",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClientPostLogoutRedirectUris_ClientId",
            schema: "IdpServer.Config",
            table: "ClientPostLogoutRedirectUris",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClientProperties_ClientId",
            schema: "IdpServer.Config",
            table: "ClientProperties",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClientRedirectUris_ClientId",
            schema: "IdpServer.Config",
            table: "ClientRedirectUris",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_Clients_ClientId",
            schema: "IdpServer.Config",
            table: "Clients",
            column: "ClientId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClientScopes_ClientId",
            schema: "IdpServer.Config",
            table: "ClientScopes",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClientSecrets_ClientId",
            schema: "IdpServer.Config",
            table: "ClientSecrets",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_IdentityResourceClaims_IdentityResourceId",
            schema: "IdpServer.Config",
            table: "IdentityResourceClaims",
            column: "IdentityResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_IdentityResourceProperties_IdentityResourceId",
            schema: "IdpServer.Config",
            table: "IdentityResourceProperties",
            column: "IdentityResourceId");

        migrationBuilder.CreateIndex(
            name: "IX_IdentityResources_Name",
            schema: "IdpServer.Config",
            table: "IdentityResources",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ApiResourceClaims",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ApiResourceProperties",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ApiResourceScopes",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ApiResourceSecrets",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ApiScopeClaims",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ApiScopeProperties",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientClaims",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientCorsOrigins",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientGrantTypes",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientIdPRestrictions",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientPostLogoutRedirectUris",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientProperties",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientRedirectUris",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientScopes",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ClientSecrets",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "IdentityResourceClaims",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "IdentityResourceProperties",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ApiResources",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "ApiScopes",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "Clients",
            schema: "IdpServer.Config");

        migrationBuilder.DropTable(
            name: "IdentityResources",
            schema: "IdpServer.Config");
    }
}
