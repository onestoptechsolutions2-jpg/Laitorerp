using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add PriceListId columns to Quotes and Proposals tables
            migrationBuilder.AddColumn<Guid>(
                name: "PriceListId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceListId",
                table: "Proposals",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "Quotes");
        }
    }
}
