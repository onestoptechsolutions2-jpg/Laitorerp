using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRateCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceCatalogItemId",
                table: "QuoteLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "PriceListItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "RateType",
                table: "PriceListItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceCatalogItemId",
                table: "PriceListItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceCatalogItemId",
                table: "OrderLines",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceCatalogItemId",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "RateType",
                table: "PriceListItems");

            migrationBuilder.DropColumn(
                name: "ServiceCatalogItemId",
                table: "PriceListItems");

            migrationBuilder.DropColumn(
                name: "ServiceCatalogItemId",
                table: "OrderLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "PriceListItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
