using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyPayloadColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadHash",
                table: "IdempotencyKey",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponseBody",
                table: "IdempotencyKey",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayloadHash",
                table: "IdempotencyKey");

            migrationBuilder.DropColumn(
                name: "ResponseBody",
                table: "IdempotencyKey");
        }
    }
}
