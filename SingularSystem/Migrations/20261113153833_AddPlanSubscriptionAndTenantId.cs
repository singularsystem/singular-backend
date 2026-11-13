using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SingularSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanSubscriptionAndTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "BaseDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanSubscriptions",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PlanType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuotaDocuments = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    ExternalPaymentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanSubscriptions", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanSubscriptions_TenantId_Status",
                table: "PlanSubscriptions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanSubscriptions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BaseDocuments");
        }
    }
}
