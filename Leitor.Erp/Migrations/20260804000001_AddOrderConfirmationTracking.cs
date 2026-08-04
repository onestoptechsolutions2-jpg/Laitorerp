using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Leitor.Erp.Migrations
{
    public partial class AddOrderConfirmationTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>("ConfirmedByUserId", "Orders", "uuid", nullable: true);
            migrationBuilder.AddColumn<DateTime>("ConfirmedAt", "Orders", "timestamp without time zone", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("ConfirmedByUserId", "Orders");
            migrationBuilder.DropColumn("ConfirmedAt", "Orders");
        }
    }
}
