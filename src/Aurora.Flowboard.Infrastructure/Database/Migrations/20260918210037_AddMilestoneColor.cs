using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                schema: "flowboard",
                table: "milestones",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Existing milestones inherit their project's color.
            migrationBuilder.Sql(
                """
                UPDATE flowboard.milestones m
                SET color = p.color
                FROM flowboard.projects p
                WHERE p.id = m.project_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                schema: "flowboard",
                table: "milestones",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color",
                schema: "flowboard",
                table: "milestones");
        }
    }
}
