using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadImportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cluster",
                table: "Leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estate",
                table: "Leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountNumber",
                table: "Leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalTicketNumber",
                table: "Leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Territory",
                table: "Leads",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cluster",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Estate",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ExternalAccountNumber",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ExternalTicketNumber",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Territory",
                table: "Leads");
        }
    }
}
