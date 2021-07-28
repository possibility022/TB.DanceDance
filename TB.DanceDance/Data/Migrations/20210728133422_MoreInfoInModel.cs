using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TB.DanceDance.Data.Migrations
{
    public partial class MoreInfoInModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "VideosInformation",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AddColumn<byte[]>(
                name: "MetadataAsJson",
                table: "VideosInformation",
                type: "varbinary(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetadataAsJson",
                table: "VideosInformation");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "VideosInformation",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);
        }
    }
}
