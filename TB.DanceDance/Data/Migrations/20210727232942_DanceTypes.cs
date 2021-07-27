using Microsoft.EntityFrameworkCore.Migrations;

namespace TB.DanceDance.Data.Migrations
{
    public partial class DanceTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "VideosInformation",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "VideosInformation");
        }
    }
}
