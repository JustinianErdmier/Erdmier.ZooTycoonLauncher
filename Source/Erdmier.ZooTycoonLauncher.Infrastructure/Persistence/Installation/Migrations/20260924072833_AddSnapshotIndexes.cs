using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_Kind_CapturedUtc",
                table: "Snapshots",
                columns: new[] { "Kind", "CapturedUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_Kind_Current",
                table: "Snapshots",
                column: "Kind",
                unique: true,
                filter: "\"Kind\" = 'Current'");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_Kind_Original",
                table: "Snapshots",
                column: "Kind",
                unique: true,
                filter: "\"Kind\" = 'Original'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Snapshots_Kind_CapturedUtc",
                table: "Snapshots");

            migrationBuilder.DropIndex(
                name: "IX_Snapshots_Kind_Current",
                table: "Snapshots");

            migrationBuilder.DropIndex(
                name: "IX_Snapshots_Kind_Original",
                table: "Snapshots");
        }
    }
}
