using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteProposalPriceListId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
