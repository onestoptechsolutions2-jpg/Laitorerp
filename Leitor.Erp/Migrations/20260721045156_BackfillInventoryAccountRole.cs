using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class BackfillInventoryAccountRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SystemAccountRole.Inventory (6) didn't exist when ErpChartOfAccountsDataSeeder first
            // ran on any database seeded before this migration, so the "1200 Inventory" account it
            // created was left with SystemRole = None (0) - backfill it here rather than relying on
            // the seeder, which only ever inserts once per empty table. Scoped to Code = '1200' and
            // the still-None role so it's a no-op on a fresh database (seeder now inserts the role
            // directly) and never overwrites a role an admin deliberately reassigned since.
            migrationBuilder.Sql(
                "UPDATE \"Accounts\" SET \"SystemRole\" = 6 WHERE \"Code\" = '1200' AND \"SystemRole\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Accounts\" SET \"SystemRole\" = 0 WHERE \"Code\" = '1200' AND \"SystemRole\" = 6;");
        }
    }
}
