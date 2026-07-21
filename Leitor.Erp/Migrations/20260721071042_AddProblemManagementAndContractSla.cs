using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemManagementAndContractSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProblemId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaHighHours",
                table: "CustomerContracts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaLowHours",
                table: "CustomerContracts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaMediumHours",
                table: "CustomerContracts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaUrgentHours",
                table: "CustomerContracts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RootCause = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Workaround = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdentifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_Problems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ProblemId",
                table: "Tickets",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_ProblemNumber",
                table: "Problems",
                column: "ProblemNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Problems");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ProblemId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ProblemId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SlaHighHours",
                table: "CustomerContracts");

            migrationBuilder.DropColumn(
                name: "SlaLowHours",
                table: "CustomerContracts");

            migrationBuilder.DropColumn(
                name: "SlaMediumHours",
                table: "CustomerContracts");

            migrationBuilder.DropColumn(
                name: "SlaUrgentHours",
                table: "CustomerContracts");
        }
    }
}
