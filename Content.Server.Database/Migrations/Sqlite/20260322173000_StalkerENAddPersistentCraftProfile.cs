using System;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    [DbContext(typeof(SqliteServerDbContext))]
    [Migration("20260322173000_StalkerENAddPersistentCraftProfile")]
    public sealed class StalkerENAddPersistentCraftProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stalker_persistent_craft_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    character_name = table.Column<string>(type: "TEXT", nullable: false),
                    available_points = table.Column<int>(type: "INTEGER", nullable: false),
                    spent_points = table.Column<int>(type: "INTEGER", nullable: false),
                    last_rewarded_round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unlocked_nodes_json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stalker_persistent_craft_profiles", x => new { x.user_id, x.character_name });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stalker_persistent_craft_profiles");
        }
    }
}
