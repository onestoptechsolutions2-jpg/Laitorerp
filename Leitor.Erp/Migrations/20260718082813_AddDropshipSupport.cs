using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddDropshipSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShipToCustomer",
                table: "PurchaseOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceOrderId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                table: "FieldServiceJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorSku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: true),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ProductVendors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SourceOrderId",
                table: "PurchaseOrders",
                column: "SourceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldServiceJobs_VendorId",
                table: "FieldServiceJobs",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendors_ProductId",
                table: "ProductVendors",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendors_VendorId",
                table: "ProductVendors",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVendors");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SourceOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_FieldServiceJobs_VendorId",
                table: "FieldServiceJobs");

            migrationBuilder.DropColumn(
                name: "ShipToCustomer",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SourceOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "FieldServiceJobs");
        }
    }
}
