using Microsoft.EntityFrameworkCore.Migrations;

namespace PokeSync.Infrastructure.Migrations
{
    public partial class SystemConfigMigration : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.CreateTable(
                name: "SystemConfig",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BootstrapInProgress = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table => { table.PrimaryKey("PK_SystemConfig", x => x.Id); });

            // Seed la ligne singleton
            m.InsertData("SystemConfig", new[] { "Id", "BootstrapInProgress" }, new object[] { 1, false });
        }

        protected override void Down(MigrationBuilder m)
        {
            m.DropTable("SystemConfig");
        }
    }
}