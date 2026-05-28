using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation.Migrations
{
    /// <inheritdoc />
    public partial class InitialInstallationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameInstallations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HasExe = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasIni = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastOpenedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastPlayedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Path = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
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
                    CloseAfterGameLaunch = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultInstallationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LauncherStartupPreference = table.Column<string>(type: "TEXT", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "System")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LauncherSettings", x => x.Id);
                    table.CheckConstraint("CK_LauncherSettings_SingletonRow", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapturedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    StructureBlob = table.Column<string>(type: "TEXT", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IniValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Section = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    ValueKind = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IniValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IniValues_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_IniValues_SnapshotId_Section_Key",
                table: "IniValues",
                columns: new[] { "SnapshotId", "Section", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameInstallations");

            migrationBuilder.DropTable(
                name: "IniValues");

            migrationBuilder.DropTable(
                name: "LauncherSettings");

            migrationBuilder.DropTable(
                name: "Snapshots");
        }
    }
}
