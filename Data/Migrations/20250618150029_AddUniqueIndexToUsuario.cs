using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TennisITAM.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cu",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_cu",
                table: "AspNetUsers",
                column: "cu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_cu",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "cu",
                table: "AspNetUsers");
        }
    }
}
