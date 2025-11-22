using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_MoMoRequestId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_MoMoTransId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MoMoRequestId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "MoMoTransId",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<string>(
                name: "GatewayToken",
                table: "PaymentTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayTransactionId",
                table: "PaymentTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_GatewayToken",
                table: "PaymentTransactions",
                column: "GatewayToken");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_GatewayTransactionId",
                table: "PaymentTransactions",
                column: "GatewayTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_GatewayToken",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_GatewayTransactionId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "GatewayToken",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "GatewayTransactionId",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<string>(
                name: "MoMoRequestId",
                table: "PaymentTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MoMoTransId",
                table: "PaymentTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MoMoRequestId",
                table: "PaymentTransactions",
                column: "MoMoRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MoMoTransId",
                table: "PaymentTransactions",
                column: "MoMoTransId",
                unique: true);
        }
    }
}
