using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizAttemptResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempt_Enrollments_EnrollmentId",
                table: "QuizAttempt");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempt_Quizzes_QuizId",
                table: "QuizAttempt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuizAttempt",
                table: "QuizAttempt");

            migrationBuilder.RenameTable(
                name: "QuizAttempt",
                newName: "QuizAttempts");

            migrationBuilder.RenameIndex(
                name: "IX_QuizAttempt_QuizId",
                table: "QuizAttempts",
                newName: "IX_QuizAttempts_QuizId");

            migrationBuilder.RenameIndex(
                name: "IX_QuizAttempt_EnrollmentId",
                table: "QuizAttempts",
                newName: "IX_QuizAttempts_EnrollmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuizAttempts",
                table: "QuizAttempts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "QuizAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    QuizAttemptId = table.Column<string>(type: "text", nullable: false),
                    QuestionId = table.Column<string>(type: "text", nullable: false),
                    SelectedOptionId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAttemptAnswers_QuestionOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizAttemptAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizAttemptAnswers_QuizAttempts_QuizAttemptId",
                        column: x => x.QuizAttemptId,
                        principalTable: "QuizAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttemptAnswers_QuestionId",
                table: "QuizAttemptAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttemptAnswers_QuizAttemptId",
                table: "QuizAttemptAnswers",
                column: "QuizAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttemptAnswers_SelectedOptionId",
                table: "QuizAttemptAnswers",
                column: "SelectedOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Enrollments_EnrollmentId",
                table: "QuizAttempts",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Enrollments_EnrollmentId",
                table: "QuizAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts");

            migrationBuilder.DropTable(
                name: "QuizAttemptAnswers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuizAttempts",
                table: "QuizAttempts");

            migrationBuilder.RenameTable(
                name: "QuizAttempts",
                newName: "QuizAttempt");

            migrationBuilder.RenameIndex(
                name: "IX_QuizAttempts_QuizId",
                table: "QuizAttempt",
                newName: "IX_QuizAttempt_QuizId");

            migrationBuilder.RenameIndex(
                name: "IX_QuizAttempts_EnrollmentId",
                table: "QuizAttempt",
                newName: "IX_QuizAttempt_EnrollmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuizAttempt",
                table: "QuizAttempt",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempt_Enrollments_EnrollmentId",
                table: "QuizAttempt",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempt_Quizzes_QuizId",
                table: "QuizAttempt",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
