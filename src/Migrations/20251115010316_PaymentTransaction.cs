using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class PaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "MoMoRequestId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<string>(type: "text", nullable: false),
                    MoMoTransId = table.Column<string>(type: "text", nullable: false),
                    MoMoRequestId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GatewayResponse = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MoMoRequestId",
                table: "PaymentTransactions",
                column: "MoMoRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MoMoTransId",
                table: "PaymentTransactions",
                column: "MoMoTransId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderId",
                table: "PaymentTransactions",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MoMoRequestId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: "Orders",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
