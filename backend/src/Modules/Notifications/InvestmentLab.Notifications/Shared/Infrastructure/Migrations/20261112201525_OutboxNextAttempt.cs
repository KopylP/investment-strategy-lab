using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Notifications.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutboxNextAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at",
                schema: "notifications",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE notifications.outbox_messages SET attempts = 24 WHERE processed_at IS NULL AND attempts >= 10;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                schema: "notifications",
                table: "outbox_messages");
        }
    }
}
