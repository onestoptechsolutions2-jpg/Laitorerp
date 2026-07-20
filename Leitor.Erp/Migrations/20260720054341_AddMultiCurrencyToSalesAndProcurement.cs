using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCurrencyToSalesAndProcurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "VendorPayments",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "VendorPayments",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "SupplierInvoices",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "SupplierInvoices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Quotes",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "Quotes",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "PurchaseOrders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "PurchaseOrders",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Payments",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "Payments",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Orders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "Orders",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Invoices",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "Invoices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "VendorPayments");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "VendorPayments");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "Invoices");
        }
    }
}
