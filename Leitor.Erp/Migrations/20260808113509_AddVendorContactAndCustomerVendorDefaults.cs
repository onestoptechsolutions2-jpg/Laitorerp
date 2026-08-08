using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorContactAndCustomerVendorDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This dev database has drift from ad-hoc changes made outside any migration before
            // this one was authored (ConfirmedAt/ConfirmedByUserId on Orders, at minimum, were
            // already present) - every step here is guarded with IF NOT EXISTS, same reasoning as
            // 20260805000001_AddPriceListIdColumns's fix, rather than relying on EF's plain
            // generated AddColumn/CreateTable, which assumes a clean baseline.
            migrationBuilder.Sql(@"ALTER TABLE ""Vendors"" ADD COLUMN IF NOT EXISTS ""DefaultCurrencyCode"" character varying(8) NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Vendors"" ADD COLUMN IF NOT EXISTS ""DefaultPaymentTerms"" integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql(@"ALTER TABLE ""Quotes"" ADD COLUMN IF NOT EXISTS ""SalespersonUserId"" uuid NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""ConfirmedAt"" timestamp without time zone NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""ConfirmedByUserId"" uuid NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""SalespersonUserId"" uuid NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""SalespersonUserId"" uuid NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""CreditLimit"" numeric(18,2) NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""DefaultCurrencyCode"" character varying(8) NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""DiscountPercent"" numeric(5,2) NOT NULL DEFAULT 0;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""CustomerPriceLists"" (
                    ""Id"" uuid NOT NULL,
                    ""CustomerId"" uuid NOT NULL,
                    ""PriceListId"" uuid NOT NULL,
                    ""IsPrimary"" boolean NOT NULL,
                    ""ExtraProperties"" text NOT NULL,
                    ""ConcurrencyStamp"" character varying(40) NOT NULL,
                    ""CreationTime"" timestamp without time zone NOT NULL,
                    ""CreatorId"" uuid NULL,
                    ""LastModificationTime"" timestamp without time zone NULL,
                    ""LastModifierId"" uuid NULL,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    ""DeleterId"" uuid NULL,
                    ""DeletionTime"" timestamp without time zone NULL,
                    CONSTRAINT ""PK_CustomerPriceLists"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""VendorContacts"" (
                    ""Id"" uuid NOT NULL,
                    ""VendorId"" uuid NOT NULL,
                    ""FullName"" character varying(256) NOT NULL,
                    ""JobTitle"" character varying(128) NULL,
                    ""Email"" character varying(256) NULL,
                    ""PhoneNumber"" character varying(32) NULL,
                    ""IsPrimary"" boolean NOT NULL,
                    ""Notes"" character varying(2000) NULL,
                    ""ExtraProperties"" text NOT NULL,
                    ""ConcurrencyStamp"" character varying(40) NOT NULL,
                    ""CreationTime"" timestamp without time zone NOT NULL,
                    ""CreatorId"" uuid NULL,
                    ""LastModificationTime"" timestamp without time zone NULL,
                    ""LastModifierId"" uuid NULL,
                    ""IsDeleted"" boolean NOT NULL DEFAULT FALSE,
                    ""DeleterId"" uuid NULL,
                    ""DeletionTime"" timestamp without time zone NULL,
                    CONSTRAINT ""PK_VendorContacts"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_VendorContacts_VendorId"" ON ""VendorContacts"" (""VendorId"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPriceLists");

            migrationBuilder.DropTable(
                name: "VendorContacts");

            migrationBuilder.DropColumn(
                name: "DefaultCurrencyCode",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "DefaultPaymentTerms",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SalespersonUserId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SalespersonUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SalespersonUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DefaultCurrencyCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Customers");
        }
    }
}
