using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Migrations
{
    /// <inheritdoc />
    public partial class InitialLauncherSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameInstallations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Path = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    HasExe = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasIni = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastPlayedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastOpenedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameInstallations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LauncherSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    LauncherStartupPreference = table.Column<string>(type: "TEXT", nullable: false),
                    CloseAfterGameLaunch = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultInstallationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Theme = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "System")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LauncherSettings", x => x.Id);
                    table.CheckConstraint("CK_LauncherSettings_SingletonRow", "\"Id\" = 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameInstallations_Name",
                table: "GameInstallations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameInstallations_Path",
                table: "GameInstallations",
                column: "Path",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameInstallations");

            migrationBuilder.DropTable(
                name: "LauncherSettings");
        }
    }
}
