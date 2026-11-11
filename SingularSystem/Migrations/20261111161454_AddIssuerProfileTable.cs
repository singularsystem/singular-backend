using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SingularSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuerProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssuerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProfessionalRegistration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuerProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuerProfiles_TenantId",
                table: "IssuerProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuerProfiles_UserId",
                table: "IssuerProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssuerProfiles");
        }
    }
}
