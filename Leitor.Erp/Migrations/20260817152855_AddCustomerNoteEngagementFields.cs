using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNoteEngagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "CustomerNotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TouchedAt",
                table: "CustomerNotes",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing rows predate TouchedAt and got the year-1 default above - backfill from
            // CreationTime (the closest available approximation of when the note was logged) so
            // they don't sort to the top of a TouchedAt DESC list ahead of everything real.
            migrationBuilder.Sql(
                "UPDATE \"CustomerNotes\" SET \"TouchedAt\" = \"CreationTime\" WHERE \"TouchedAt\" = '0001-01-01 00:00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "CustomerNotes");

            migrationBuilder.DropColumn(
                name: "TouchedAt",
                table: "CustomerNotes");
        }
    }
}
