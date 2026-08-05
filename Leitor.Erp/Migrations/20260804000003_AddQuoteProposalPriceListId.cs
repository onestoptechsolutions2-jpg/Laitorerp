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
            // Idempotent: only add columns if they don't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Quotes' AND column_name = 'PriceListId'
                    ) THEN
                        ALTER TABLE ""Quotes"" ADD COLUMN ""PriceListId"" uuid NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Proposals' AND column_name = 'PriceListId'
                    ) THEN
                        ALTER TABLE ""Proposals"" ADD COLUMN ""PriceListId"" uuid NULL;
                    END IF;
                END $$;
            ");
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
