using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppOwnsDataShared.Migrations
{
    public partial class AddTenantTheme : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoSymbol",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemePrimary",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeSecondary",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeTertiary",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoSymbol",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ThemePrimary",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ThemeSecondary",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ThemeTertiary",
                table: "Tenants");
        }
    }
}
