using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteMarginGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MarginOverrideAt",
                table: "Quotes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MarginOverrideByUserId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarginOverrideReason",
                table: "Quotes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarginOverrideAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "MarginOverrideByUserId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "MarginOverrideReason",
                table: "Quotes");
        }
    }
}
