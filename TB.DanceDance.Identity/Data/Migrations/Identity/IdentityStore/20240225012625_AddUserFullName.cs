using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Identity.Data.Migrations.Identity.IdentityStore
{
    /// <inheritdoc />
    public partial class AddUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "Idp.Ident",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "Idp.Ident",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "Idp.Ident",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "Idp.Ident",
                table: "AspNetUsers");
        }
    }
}
