using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLectureIdFromComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Lectures_LectureId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Lectures_LectureId1",
                table: "Comments");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
