using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadOutreachTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DoNotContact",
                table: "Leads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhone",
                table: "Leads",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeadTouches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TouchedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadTouches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_NormalizedPhone",
                table: "Leads",
                column: "NormalizedPhone");

            migrationBuilder.CreateIndex(
                name: "IX_LeadTouches_LeadId",
                table: "LeadTouches",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadTouches");

            migrationBuilder.DropIndex(
                name: "IX_Leads_NormalizedPhone",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "DoNotContact",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "NormalizedPhone",
                table: "Leads");
        }
    }
}
