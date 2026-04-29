using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "GiftCodes");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "GiftCodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUses",
                table: "GiftCodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "GiftCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CourseId",
                table: "Comments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Comments",
                type: "text",
                nullable: true);

            // Data Migration: Populate Comments.UserId and Comments.CourseId from Enrollments
            migrationBuilder.Sql(@"
                UPDATE ""Comments""
                SET ""UserId"" = e.""StudentId"",
                    ""CourseId"" = e.""CourseId""
                FROM ""Enrollments"" e
                WHERE ""Comments"".""EnrollmentId"" = e.""Id""
            ");

            // Data Migration: Populate GiftCodes.CreatedByUserId with the first admin found
            migrationBuilder.Sql(@"
                UPDATE ""GiftCodes""
                SET ""CreatedByUserId"" = (SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""UserType"" = 'Admin' LIMIT 1)
                WHERE ""CreatedByUserId"" IS NULL
            ");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "GiftCodes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CourseId",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "QuestionAnswers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CourseId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionAnswers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionAnswers_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionAnswers_QuestionAnswers_ParentId",
                        column: x => x.ParentId,
                        principalTable: "QuestionAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodes_CreatedByUserId",
                table: "GiftCodes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CourseId",
                table: "Comments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnswers_CourseId",
                table: "QuestionAnswers",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnswers_ParentId",
                table: "QuestionAnswers",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnswers_UserId",
                table: "QuestionAnswers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Courses_CourseId",
                table: "Comments",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GiftCodes_AspNetUsers_CreatedByUserId",
                table: "GiftCodes",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Courses_CourseId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_GiftCodes_AspNetUsers_CreatedByUserId",
                table: "GiftCodes");

            migrationBuilder.DropTable(
                name: "QuestionAnswers");

            migrationBuilder.DropIndex(
                name: "IX_GiftCodes_CreatedByUserId",
                table: "GiftCodes");

            migrationBuilder.DropIndex(
                name: "IX_Comments_CourseId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "GiftCodes");

            migrationBuilder.DropColumn(
                name: "MaxUses",
                table: "GiftCodes");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "GiftCodes");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Comments");

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "GiftCodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
