using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToVendorPartnerAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: true (not the generator's usual default(bool)=false) so every existing
            // Vendor/Partner/Agent row backfills as Active, matching the entity's own `= true`
            // default - a bare "false" here would have silently deactivated every pre-existing
            // record in the table the moment this migration ran.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Partners",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Agents",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Agents");
        }
    }
}
