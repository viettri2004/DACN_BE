using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNumberIdFromQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberId",
                table: "Quizzes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumberId",
                table: "Quizzes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
