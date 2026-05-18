using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shared.EWS.Migrations
{
    /// <inheritdoc />
    public partial class TaskTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    task_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    task_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    task_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to_user_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    task_status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    priority = table.Column<string>(type: "text", nullable: false, defaultValue: "Medium"),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.task_id);
                    table.ForeignKey(
                        name: "FK_tasks_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tasks_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tasks_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_assigned_by_user_id",
                table: "tasks",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_assigned_to_user_id",
                table: "tasks",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_project_id",
                table: "tasks",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tasks");
        }
    }
}
