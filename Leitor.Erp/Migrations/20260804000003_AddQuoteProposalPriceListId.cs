using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Leitor.Erp.Migrations
{
    public partial class AddQuoteProposalPriceListId : Migration
    {
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "Proposals");
        }
    }
}
