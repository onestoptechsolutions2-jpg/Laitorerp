using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PortalUserId",
                table: "Vendors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PortalUserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_PortalUserId",
                table: "Vendors",
                column: "PortalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PortalUserId",
                table: "Customers",
                column: "PortalUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendors_PortalUserId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PortalUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PortalUserId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PortalUserId",
                table: "Customers");
        }
    }
}
