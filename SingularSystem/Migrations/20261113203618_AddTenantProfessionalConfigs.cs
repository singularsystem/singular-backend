using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SingularSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantProfessionalConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantProfessionalConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PsychologistEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PsychopedagogueEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SocialWorkEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantProfessionalConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantProfessionalConfigs_TenantId",
                table: "TenantProfessionalConfigs",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantProfessionalConfigs");
        }
    }
}
