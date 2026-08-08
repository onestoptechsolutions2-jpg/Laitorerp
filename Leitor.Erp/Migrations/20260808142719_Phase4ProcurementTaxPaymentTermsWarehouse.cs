using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ProcurementTaxPaymentTermsWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded the same way 20260808113509_AddVendorContactAndCustomerVendorDefaults was
            // fixed - this dev database has a history of drift from ad-hoc changes made outside
            // any migration, so every step here is IF NOT EXISTS rather than trusting a clean
            // baseline.
            migrationBuilder.Sql(@"ALTER TABLE ""SupplierInvoices"" ADD COLUMN IF NOT EXISTS ""PaymentTerms"" integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql(@"ALTER TABLE ""SupplierInvoiceLines"" ADD COLUMN IF NOT EXISTS ""TaxRateId"" uuid NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""SupplierInvoiceLines"" ADD COLUMN IF NOT EXISTS ""TaxRatePercent"" numeric(5,2) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql(@"ALTER TABLE ""PurchaseOrders"" ADD COLUMN IF NOT EXISTS ""PaymentTerms"" integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql(@"ALTER TABLE ""PurchaseOrders"" ADD COLUMN IF NOT EXISTS ""WarehouseId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql(@"ALTER TABLE ""PurchaseOrderLines"" ADD COLUMN IF NOT EXISTS ""TaxRateId"" uuid NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""PurchaseOrderLines"" ADD COLUMN IF NOT EXISTS ""TaxRatePercent"" numeric(5,2) NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                table: "SupplierInvoiceLines");

            migrationBuilder.DropColumn(
                name: "TaxRatePercent",
                table: "SupplierInvoiceLines");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "TaxRatePercent",
                table: "PurchaseOrderLines");
        }
    }
}
