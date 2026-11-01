using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOD.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatForwarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "forwarded_from_id",
                table: "chat_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_forwarded_from_id",
                table: "chat_messages",
                column: "forwarded_from_id");

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_users_forwarded_from_id",
                table: "chat_messages",
                column: "forwarded_from_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chat_messages_users_forwarded_from_id",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "ix_chat_messages_forwarded_from_id",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "forwarded_from_id",
                table: "chat_messages");
        }
    }
}
