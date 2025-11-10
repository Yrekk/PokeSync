using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElementType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Generation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyKey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyKey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pokemon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    GenerationId = table.Column<int>(type: "int", nullable: false),
                    SpriteUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(7,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pokemon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pokemon_Generation_GenerationId",
                        column: x => x.GenerationId,
                        principalTable: "Generation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PokemonFlavor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PokemonId = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonFlavor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonFlavor_Pokemon_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PokemonStat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PokemonId = table.Column<int>(type: "int", nullable: false),
                    StatName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BaseValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonStat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonStat_Pokemon_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PokemonType",
                columns: table => new
                {
                    PokemonId = table.Column<int>(type: "int", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonType", x => new { x.PokemonId, x.TypeId });
                    table.ForeignKey(
                        name: "FK_PokemonType_ElementType_TypeId",
                        column: x => x.TypeId,
                        principalTable: "ElementType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PokemonType_Pokemon_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElementType_Name",
                table: "ElementType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Generation_Number",
                table: "Generation",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKey_CreatedUtc",
                table: "IdempotencyKey",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKey_ExternalKey",
                table: "IdempotencyKey",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pokemon_ExternalId",
                table: "Pokemon",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pokemon_GenerationId_Id",
                table: "Pokemon",
                columns: new[] { "GenerationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Pokemon_Name",
                table: "Pokemon",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemon_Number",
                table: "Pokemon",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonFlavor_PokemonId_Language",
                table: "PokemonFlavor",
                columns: new[] { "PokemonId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PokemonStat_PokemonId_StatName",
                table: "PokemonStat",
                columns: new[] { "PokemonId", "StatName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PokemonType_TypeId",
                table: "PokemonType",
                column: "TypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyKey");

            migrationBuilder.DropTable(
                name: "PokemonFlavor");

            migrationBuilder.DropTable(
                name: "PokemonStat");

            migrationBuilder.DropTable(
                name: "PokemonType");

            migrationBuilder.DropTable(
                name: "ElementType");

            migrationBuilder.DropTable(
                name: "Pokemon");

            migrationBuilder.DropTable(
                name: "Generation");
        }
    }
}
