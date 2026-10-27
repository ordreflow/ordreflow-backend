using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "closed_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tasks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM orders
                        WHERE status NOT IN ('Open', 'Closed')
                           OR (status = 'Closed' AND closed_at IS NULL)
                    ) THEN
                        RAISE EXCEPTION 'Cannot backfill task status from invalid order status or missing close time';
                    END IF;
                END $$;
                """);

            // Existing tasks inherit the current state and close time of their order.
            migrationBuilder.Sql("""
                UPDATE tasks AS task
                SET status = CASE WHEN ord.status = 'Closed' THEN 'Closed' ELSE 'Open' END,
                    closed_at = CASE WHEN ord.status = 'Closed' THEN ord.closed_at ELSE NULL END
                FROM orders AS ord
                WHERE task.order_id = ord.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "tasks",
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
                name: "closed_at",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tasks");
        }
    }
}
