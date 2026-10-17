using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "flowboard",
                table: "work_items",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "flowboard",
                table: "users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "flowboard",
                table: "projects",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "flowboard",
                table: "milestones",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "flowboard",
                table: "components",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "flowboard",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "flowboard",
                table: "users");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "flowboard",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "flowboard",
                table: "milestones");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "flowboard",
                table: "components");
        }
    }
}
