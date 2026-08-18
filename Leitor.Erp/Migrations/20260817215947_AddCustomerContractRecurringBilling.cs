using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerContractRecurringBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingFrequency",
                table: "CustomerContracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBilledDate",
                table: "CustomerContracts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextBillingDate",
                table: "CustomerContracts",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingFrequency",
                table: "CustomerContracts");

            migrationBuilder.DropColumn(
                name: "LastBilledDate",
                table: "CustomerContracts");

            migrationBuilder.DropColumn(
                name: "NextBillingDate",
                table: "CustomerContracts");
        }
    }
}
