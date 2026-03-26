using Content.Server.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    [DbContext(typeof(PostgresServerDbContext))]
    [Migration("20260327120010_StalkerENPersistentCraftProfileJsonOnly")]
    public sealed class StalkerENPersistentCraftProfileJsonOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "unlocked_nodes_json",
                table: "stalker_persistent_craft_profiles",
                newName: "profile_json");

            migrationBuilder.DropColumn(
                name: "available_points",
                table: "stalker_persistent_craft_profiles");

            migrationBuilder.DropColumn(
                name: "spent_points",
                table: "stalker_persistent_craft_profiles");

            migrationBuilder.DropColumn(
                name: "last_rewarded_round_id",
                table: "stalker_persistent_craft_profiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "available_points",
                table: "stalker_persistent_craft_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "spent_points",
                table: "stalker_persistent_craft_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "last_rewarded_round_id",
                table: "stalker_persistent_craft_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameColumn(
                name: "profile_json",
                table: "stalker_persistent_craft_profiles",
                newName: "unlocked_nodes_json");
        }
    }
}
