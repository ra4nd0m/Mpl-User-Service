using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MplDbApi.Migrations
{
    /// <inheritdoc />
    public partial class FilterCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Filters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AffectedRole = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Groups = table.Column<string>(type: "TEXT", nullable: true),
                    Sources = table.Column<string>(type: "TEXT", nullable: true),
                    Units = table.Column<string>(type: "TEXT", nullable: true),
                    MaterialIds = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filters", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Filters");
        }
    }
}
