using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MplUserService.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupToFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileGroup",
                table: "ReportFiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileGroup",
                table: "ReportFiles");
        }
    }
}
