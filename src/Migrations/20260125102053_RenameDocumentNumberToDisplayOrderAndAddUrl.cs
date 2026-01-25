using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class RenameDocumentNumberToDisplayOrderAndAddUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DocumentNumber",
                table: "Documents",
                newName: "DisplayOrder");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "Documents",
                newName: "DocumentNumber");
        }
    }
}
