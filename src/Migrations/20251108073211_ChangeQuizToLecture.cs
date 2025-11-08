using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class ChangeQuizToLecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Courses_CourseId",
                table: "Quizzes");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Quizzes",
                newName: "LectureId");

            migrationBuilder.RenameIndex(
                name: "IX_Quizzes_CourseId",
                table: "Quizzes",
                newName: "IX_Quizzes_LectureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Lectures_LectureId",
                table: "Quizzes",
                column: "LectureId",
                principalTable: "Lectures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Lectures_LectureId",
                table: "Quizzes");

            migrationBuilder.RenameColumn(
                name: "LectureId",
                table: "Quizzes",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_Quizzes_LectureId",
                table: "Quizzes",
                newName: "IX_Quizzes_CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Courses_CourseId",
                table: "Quizzes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
