using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class NameToQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Content",
                table: "LectureVideos",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Quizzes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Quizzes");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "LectureVideos",
                newName: "Content");
        }
    }
}
