using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixtureHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeIdempotencyKeysByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "IdempotencyRecords",
                type: "nvarchar(137)",
                maxLength: 137,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "IdempotencyRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(137)",
                oldMaxLength: 137);
        }
    }
}
