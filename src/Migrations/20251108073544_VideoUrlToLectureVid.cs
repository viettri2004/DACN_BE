using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class VideoUrlToLectureVid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_LectureVideos_LectureVideoId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureVideos_Comments_ParentId",
                table: "LectureVideos");

            migrationBuilder.DropIndex(
                name: "IX_LectureVideos_ParentId",
                table: "LectureVideos");

            migrationBuilder.DropIndex(
                name: "IX_Comments_LectureVideoId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "LectureVideos");

            migrationBuilder.DropColumn(
                name: "ReplyId",
                table: "LectureVideos");

            migrationBuilder.DropColumn(
                name: "LectureVideoId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "LectureVideos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "LectureVideos");

            migrationBuilder.AddColumn<string>(
                name: "ParentId",
                table: "LectureVideos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplyId",
                table: "LectureVideos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LectureVideoId",
                table: "Comments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectureVideos_ParentId",
                table: "LectureVideos",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_LectureVideoId",
                table: "Comments",
                column: "LectureVideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_LectureVideos_LectureVideoId",
                table: "Comments",
                column: "LectureVideoId",
                principalTable: "LectureVideos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LectureVideos_Comments_ParentId",
                table: "LectureVideos",
                column: "ParentId",
                principalTable: "Comments",
                principalColumn: "Id");
        }
    }
}
