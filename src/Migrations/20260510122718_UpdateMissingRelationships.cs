using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMissingRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Lectures_LectureId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "LectureId1",
                table: "Comments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_LectureId1",
                table: "Comments",
                column: "LectureId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Lectures_LectureId",
                table: "Comments",
                column: "LectureId",
                principalTable: "Lectures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Lectures_LectureId1",
                table: "Comments",
                column: "LectureId1",
                principalTable: "Lectures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLectureProgresses_AspNetUsers_StudentId",
                table: "StudentLectureProgresses",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Lectures_LectureId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Lectures_LectureId1",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentLectureProgresses_AspNetUsers_StudentId",
                table: "StudentLectureProgresses");

            migrationBuilder.DropIndex(
                name: "IX_Comments_LectureId1",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "LectureId1",
                table: "Comments");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Lectures_LectureId",
                table: "Comments",
                column: "LectureId",
                principalTable: "Lectures",
                principalColumn: "Id");
        }
    }
}
