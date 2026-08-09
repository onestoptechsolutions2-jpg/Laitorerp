using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddCybersecurityLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContainedDate",
                table: "Tickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSecurityBreach",
                table: "Tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ConvertedToContractId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServicesIncluded",
                table: "CustomerContracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasEndpointProtection",
                table: "ConfigurationItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBackedUp",
                table: "ConfigurationItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBackupVerifiedDate",
                table: "ConfigurationItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPatchedDate",
                table: "ConfigurationItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SecurityMonitoringEnabled",
                table: "ConfigurationItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SecurityAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConductedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RiskRating = table.Column<int>(type: "integer", nullable: true),
                    Findings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Recommendations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_SecurityAssessments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAssessments_AssessmentNumber",
                table: "SecurityAssessments",
                column: "AssessmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAssessments_CustomerId",
                table: "SecurityAssessments",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAssessments");

            migrationBuilder.DropColumn(
                name: "ContainedDate",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsSecurityBreach",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ConvertedToContractId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ServicesIncluded",
                table: "CustomerContracts");

            migrationBuilder.DropColumn(
                name: "HasEndpointProtection",
                table: "ConfigurationItems");

            migrationBuilder.DropColumn(
                name: "IsBackedUp",
                table: "ConfigurationItems");

            migrationBuilder.DropColumn(
                name: "LastBackupVerifiedDate",
                table: "ConfigurationItems");

            migrationBuilder.DropColumn(
                name: "LastPatchedDate",
                table: "ConfigurationItems");

            migrationBuilder.DropColumn(
                name: "SecurityMonitoringEnabled",
                table: "ConfigurationItems");
        }
    }
}
