using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantGiftCodeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GiftCodes_AspNetUsers_CreatedByUserId",
                table: "GiftCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_GiftCodes_AspNetUsers_UsedByStudentId",
                table: "GiftCodes");

            migrationBuilder.DropIndex(
                name: "IX_GiftCodes_CreatedByUserId",
                table: "GiftCodes");

            migrationBuilder.DropIndex(
                name: "IX_GiftCodes_UsedByStudentId",
                table: "GiftCodes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "GiftCodes");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "GiftCodes");

            migrationBuilder.DropColumn(
                name: "UsedByStudentId",
                table: "GiftCodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "GiftCodes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "GiftCodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsedByStudentId",
                table: "GiftCodes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodes_CreatedByUserId",
                table: "GiftCodes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodes_UsedByStudentId",
                table: "GiftCodes",
                column: "UsedByStudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_GiftCodes_AspNetUsers_CreatedByUserId",
                table: "GiftCodes",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GiftCodes_AspNetUsers_UsedByStudentId",
                table: "GiftCodes",
                column: "UsedByStudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
