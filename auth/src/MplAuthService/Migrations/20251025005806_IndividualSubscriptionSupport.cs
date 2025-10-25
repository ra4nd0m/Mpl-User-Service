using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MplAuthService.Migrations
{
    /// <inheritdoc />
    public partial class IndividualSubscriptionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IndividualSubscriptionId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IndividualSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SubscriptionType = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubscriptionEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndividualSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IndividualSubscriptionId",
                table: "AspNetUsers",
                column: "IndividualSubscriptionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_IndividualSubscriptions_IndividualSubscriptionId",
                table: "AspNetUsers",
                column: "IndividualSubscriptionId",
                principalTable: "IndividualSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_IndividualSubscriptions_IndividualSubscriptionId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "IndividualSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IndividualSubscriptionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IndividualSubscriptionId",
                table: "AspNetUsers");
        }
    }
}
