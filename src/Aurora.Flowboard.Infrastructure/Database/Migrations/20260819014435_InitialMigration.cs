using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "flowboard");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "flowboard",
                columns: table => new
                {
                    outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.outbox_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    prefix = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    work_item_counter = table.Column<int>(type: "integer", nullable: false),
                    last_activity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "template_flows",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_template_flows", x => x.id);
                    table.ForeignKey(
                        name: "fk_template_flows_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "flowboard",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.name });
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "flowboard",
                columns: table => new
                {
                    user_token_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    access_token_expires_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    refresh_token_expires_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issued_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => x.user_token_id);
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "components",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_components_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "flowboard",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_components_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "flow_states",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flow_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_flow_states_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "flowboard",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestones",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    target_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    target_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_milestones", x => x.id);
                    table.ForeignKey(
                        name: "fk_milestones_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "flowboard",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_milestones_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_change_logs",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    affected_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_status = table.Column<int>(type: "integer", nullable: true),
                    new_kind = table.Column<string>(type: "text", nullable: true),
                    changed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_change_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_change_logs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "flowboard",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_change_logs_users_changed_by_id",
                        column: x => x.changed_by_id,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    joined_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "flowboard",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "flowboard",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "template_flow_states",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_flow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_template_flow_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_template_flow_states_template_flows_template_flow_id",
                        column: x => x.template_flow_id,
                        principalSchema: "flowboard",
                        principalTable: "template_flows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flow_transitions",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_roles = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flow_transitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_flow_transitions_flow_states_from_state_id",
                        column: x => x.from_state_id,
                        principalSchema: "flowboard",
                        principalTable: "flow_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_flow_transitions_flow_states_to_state_id",
                        column: x => x.to_state_id,
                        principalSchema: "flowboard",
                        principalTable: "flow_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_flow_transitions_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "flowboard",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_items",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "text", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flow_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    estimated_points = table.Column<int>(type: "integer", nullable: true),
                    estimated_completion_date = table.Column<DateOnly>(type: "date", nullable: true),
                    component_id = table.Column<Guid>(type: "uuid", nullable: true),
                    milestone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_items_components_component_id",
                        column: x => x.component_id,
                        principalSchema: "flowboard",
                        principalTable: "components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_items_flow_states_flow_state_id",
                        column: x => x.flow_state_id,
                        principalSchema: "flowboard",
                        principalTable: "flow_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_items_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalSchema: "flowboard",
                        principalTable: "milestones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_items_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "flowboard",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_change_logs",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    affected_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_item_change_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_item_change_logs_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalSchema: "flowboard",
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_comments",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_item_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_item_comments_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalSchema: "flowboard",
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_state_transition_histories",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_state_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_item_state_transition_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_item_state_transition_histories_flow_states_from_state",
                        column: x => x.from_state_id,
                        principalSchema: "flowboard",
                        principalTable: "flow_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_item_state_transition_histories_flow_states_to_state_id",
                        column: x => x.to_state_id,
                        principalSchema: "flowboard",
                        principalTable: "flow_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_item_state_transition_histories_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalSchema: "flowboard",
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_tags",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_item_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_item_tags_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalSchema: "flowboard",
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_time_entries",
                schema: "flowboard",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    logged_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_item_time_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_item_time_entries_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalSchema: "flowboard",
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_components_created_by",
                schema: "flowboard",
                table: "components",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_components_project_id",
                schema: "flowboard",
                table: "components",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_components_project_id_name",
                schema: "flowboard",
                table: "components",
                columns: new[] { "project_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_flow_states_project_id",
                schema: "flowboard",
                table: "flow_states",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_flow_transitions_from_state_id",
                schema: "flowboard",
                table: "flow_transitions",
                column: "from_state_id");

            migrationBuilder.CreateIndex(
                name: "ix_flow_transitions_project_id",
                schema: "flowboard",
                table: "flow_transitions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_flow_transitions_project_id_from_state_id_to_state_id",
                schema: "flowboard",
                table: "flow_transitions",
                columns: new[] { "project_id", "from_state_id", "to_state_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_flow_transitions_to_state_id",
                schema: "flowboard",
                table: "flow_transitions",
                column: "to_state_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_created_by",
                schema: "flowboard",
                table: "milestones",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_project_id",
                schema: "flowboard",
                table: "milestones",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_project_id_name",
                schema: "flowboard",
                table: "milestones",
                columns: new[] { "project_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_change_logs_changed_by_id",
                schema: "flowboard",
                table: "project_change_logs",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_change_logs_project_id",
                schema: "flowboard",
                table: "project_change_logs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_project_id",
                schema: "flowboard",
                table: "project_members",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_project_id_user_id",
                schema: "flowboard",
                table: "project_members",
                columns: new[] { "project_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_members_user_id",
                schema: "flowboard",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_created_by",
                schema: "flowboard",
                table: "projects",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_projects_prefix",
                schema: "flowboard",
                table: "projects",
                column: "prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_template_flow_states_template_flow_id",
                schema: "flowboard",
                table: "template_flow_states",
                column: "template_flow_id");

            migrationBuilder.CreateIndex(
                name: "ix_template_flows_created_by",
                schema: "flowboard",
                table: "template_flows",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_template_flows_kind",
                schema: "flowboard",
                table: "template_flows",
                column: "kind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_refresh_token",
                schema: "flowboard",
                table: "user_tokens",
                column: "refresh_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_user_id",
                schema: "flowboard",
                table: "user_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "flowboard",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_item_change_logs_work_item_id",
                schema: "flowboard",
                table: "work_item_change_logs",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_item_comments_work_item_id",
                schema: "flowboard",
                table: "work_item_comments",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_item_state_transition_histories_from_state_id",
                schema: "flowboard",
                table: "work_item_state_transition_histories",
                column: "from_state_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_item_state_transition_histories_to_state_id",
                schema: "flowboard",
                table: "work_item_state_transition_histories",
                column: "to_state_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_item_state_transition_histories_work_item_id",
                schema: "flowboard",
                table: "work_item_state_transition_histories",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_item_tags_work_item_id_name",
                schema: "flowboard",
                table: "work_item_tags",
                columns: new[] { "work_item_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_item_time_entries_work_item_id",
                schema: "flowboard",
                table: "work_item_time_entries",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_code",
                schema: "flowboard",
                table: "work_items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_items_component_id",
                schema: "flowboard",
                table: "work_items",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_flow_state_id",
                schema: "flowboard",
                table: "work_items",
                column: "flow_state_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_milestone_id",
                schema: "flowboard",
                table: "work_items",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_project_id",
                schema: "flowboard",
                table: "work_items",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flow_transitions",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "project_change_logs",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "project_members",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "template_flow_states",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "work_item_change_logs",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "work_item_comments",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "work_item_state_transition_histories",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "work_item_tags",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "work_item_time_entries",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "template_flows",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "work_items",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "components",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "flow_states",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "milestones",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "flowboard");

            migrationBuilder.DropTable(
                name: "users",
                schema: "flowboard");
        }
    }
}
