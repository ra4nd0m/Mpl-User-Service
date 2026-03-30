using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MplDbApi.Migrations.BMplbase
{
    /// <inheritdoc />
    public partial class AddDescriptionAndRoundToToMaterialSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "material_source",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "round_to",
                table: "material_source",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "material_source");

            migrationBuilder.DropColumn(
                name: "round_to",
                table: "material_source");
        }
    }
}
