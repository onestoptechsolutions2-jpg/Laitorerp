using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WithholdingTaxRateId",
                table: "Vendors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingTaxAmount",
                table: "VendorPayments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TaxType",
                table: "TaxRates",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithholdingTaxRateId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxAmount",
                table: "VendorPayments");

            migrationBuilder.DropColumn(
                name: "TaxType",
                table: "TaxRates");
        }
    }
}
