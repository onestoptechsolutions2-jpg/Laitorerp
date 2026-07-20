using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalEntryReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReversedEntryId",
                table: "JournalEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ReversedEntryId",
                table: "JournalEntries",
                column: "ReversedEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ReversedEntryId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ReversedEntryId",
                table: "JournalEntries");
        }
    }
}
