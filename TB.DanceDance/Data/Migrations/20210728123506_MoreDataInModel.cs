using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TB.DanceDance.Data.Migrations
{
    public partial class MoreDataInModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTimeUtc",
                table: "VideosInformation",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "VideosInformation",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationTimeUtc",
                table: "VideosInformation");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "VideosInformation");
        }
    }
}
