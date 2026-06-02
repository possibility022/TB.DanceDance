using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrivateVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ConvertedBlobSize",
                schema: "video",
                table: "Videos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SourceBlobSize",
                schema: "video",
                table: "Videos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StorageQuotaBytes",
                schema: "access",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedBlobSize",
                schema: "video",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "SourceBlobSize",
                schema: "video",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "StorageQuotaBytes",
                schema: "access",
                table: "Users");
        }
    }
}
