using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MplAuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddCanExportDataToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanExportData",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanExportData",
                table: "AspNetUsers");
        }
    }
}
