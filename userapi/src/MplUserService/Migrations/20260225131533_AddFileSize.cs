using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MplUserService.Migrations
{
    /// <inheritdoc />
    public partial class AddFileSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "ReportFiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "ReportFiles");
        }
    }
}
