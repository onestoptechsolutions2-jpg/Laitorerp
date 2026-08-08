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
            // Duplicates 20260804000003_AddQuoteProposalPriceListId - same two columns, added a
            // day later by mistake. That migration already made itself idempotent (IF NOT EXISTS
            // guard); this one crashed every deploy because it used a plain AddColumn against a
            // column the sibling migration had already created. Made idempotent the same way
            // rather than removed outright, since this migration may already be recorded as
            // applied in __EFMigrationsHistory on some deployed databases.
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
            // No-op: this migration never uniquely owned these columns (see Up() above), so
            // rolling it back must not drop them out from under 20260804000003's own Down(),
            // which runs afterwards and does the actual DropColumn.
        }
    }
}
