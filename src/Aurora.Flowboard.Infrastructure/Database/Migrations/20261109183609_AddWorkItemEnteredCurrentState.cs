using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemEnteredCurrentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_items_flow_state_id",
                schema: "flowboard",
                table: "work_items");

            // Added nullable, filled, then made required: the existing rows need a real value, and
            // EF's default (0001-01-01) would show every work item as stalled.
            migrationBuilder.AddColumn<DateTime>(
                name: "entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items",
                type: "timestamp with time zone",
                nullable: true);

            // The last transition is when the item entered its current state; an item never moved is
            // still in the state it was created in.
            migrationBuilder.Sql(
                """
                UPDATE flowboard.work_items w
                SET entered_current_state_on_utc = COALESCE(
                    (SELECT MAX(h.changed_on_utc)
                     FROM flowboard.work_item_state_transition_histories h
                     WHERE h.work_item_id = w.id),
                    w.created_on_utc);
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_items_flow_state_id_entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items",
                columns: new[] { "flow_state_id", "entered_current_state_on_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing is lost: the column can be computed again from the state transition history.
            migrationBuilder.DropIndex(
                name: "ix_work_items_flow_state_id_entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_flow_state_id",
                schema: "flowboard",
                table: "work_items",
                column: "flow_state_id");
        }
    }
}
