using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts",
                schema: "flowboard",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attempts",
                schema: "flowboard",
                table: "outbox_messages");
        }
    }
}
