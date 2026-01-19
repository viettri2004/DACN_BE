using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInstructorRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Evidence",
                table: "InstructorRequests",
                newName: "SocialLinks");

            migrationBuilder.AddColumn<string>(
                name: "Certificate",
                table: "InstructorRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                table: "InstructorRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Expertise",
                table: "InstructorRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Introduction",
                table: "InstructorRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Certificate",
                table: "InstructorRequests");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "InstructorRequests");

            migrationBuilder.DropColumn(
                name: "Expertise",
                table: "InstructorRequests");

            migrationBuilder.DropColumn(
                name: "Introduction",
                table: "InstructorRequests");

            migrationBuilder.RenameColumn(
                name: "SocialLinks",
                table: "InstructorRequests",
                newName: "Evidence");
        }
    }
}
