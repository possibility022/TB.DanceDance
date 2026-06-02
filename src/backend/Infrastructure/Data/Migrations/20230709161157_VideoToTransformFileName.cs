using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TB.DanceDance.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class VideoToTransformFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "video",
                table: "ToTransform",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "video",
                table: "ToTransform");
        }
    }
}
