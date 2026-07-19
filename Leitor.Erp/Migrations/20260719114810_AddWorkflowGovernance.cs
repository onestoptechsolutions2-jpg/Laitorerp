using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leitor.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnlockReason",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlockedAt",
                table: "Quotes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnlockedByUserId",
                table: "Quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Quotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UnlockReason",
                table: "Proposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlockedAt",
                table: "Proposals",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnlockedByUserId",
                table: "Proposals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockReason",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlockedAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnlockedByUserId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "OrderPaymentMilestones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LeadId",
                table: "Opportunities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowStageEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
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
                    table.PrimaryKey("PK_WorkflowStageEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_LeadId",
                table: "Opportunities",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldServiceJobs_OrderId",
                table: "FieldServiceJobs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStageEvents_EntityType_EntityId",
                table: "WorkflowStageEvents",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowStageEvents");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_LeadId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_FieldServiceJobs_OrderId",
                table: "FieldServiceJobs");

            migrationBuilder.DropColumn(
                name: "UnlockReason",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "UnlockedAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "UnlockedByUserId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "UnlockReason",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "UnlockedAt",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "UnlockedByUserId",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "UnlockReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UnlockedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UnlockedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "OrderPaymentMilestones");

            migrationBuilder.DropColumn(
                name: "LeadId",
                table: "Opportunities");
        }
    }
}
