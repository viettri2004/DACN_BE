using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDatabaseRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorRequests_AspNetUsers_AdminId",
                table: "InstructorRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorRequests_AspNetUsers_AdminId",
                table: "InstructorRequests",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorRequests_AspNetUsers_AdminId",
                table: "InstructorRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorRequests_AspNetUsers_AdminId",
                table: "InstructorRequests",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
