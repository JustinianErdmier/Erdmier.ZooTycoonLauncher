using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Migrations
{
    /// <inheritdoc />
    public partial class AddIniEditorStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IniEditorStyle",
                table: "LauncherSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "Friendly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IniEditorStyle",
                table: "LauncherSettings");
        }
    }
}
