using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLauncherTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "LauncherSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "LauncherSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "System");
        }
    }
}
