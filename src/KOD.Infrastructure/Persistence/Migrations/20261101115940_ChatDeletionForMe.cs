using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOD.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatDeletionForMe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "cleared_sequence",
                table: "chat_members",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "chat_hidden_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hidden_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_hidden_messages", x => new { x.message_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_chat_hidden_messages_chat_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_hidden_messages_chats_chat_id",
                        column: x => x.chat_id,
                        principalTable: "chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_hidden_messages_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_hidden_messages_chat_id",
                table: "chat_hidden_messages",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_hidden_messages_user_id_chat_id",
                table: "chat_hidden_messages",
                columns: new[] { "user_id", "chat_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_hidden_messages");

            migrationBuilder.DropColumn(
                name: "cleared_sequence",
                table: "chat_members");
        }
    }
}
