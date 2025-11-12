using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_SystemConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BootstrapInProgress = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfig", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemConfig",
                columns: new[] { "Id", "BootstrapInProgress", "LastSyncUtc" },
                values: new object[] { 1, false, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemConfig");
        }
    }
}
