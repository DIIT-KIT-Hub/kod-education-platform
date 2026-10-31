using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOD.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    direct_key = table.Column<string>(type: "character varying(65)", maxLength: 65, nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_members",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_read_sequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_members", x => new { x.chat_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_chat_members_chats_chat_id",
                        column: x => x.chat_id,
                        principalTable: "chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    @event = table.Column<int>(name: "event", type: "integer", nullable: true),
                    body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_messages_chats_chat_id",
                        column: x => x.chat_id,
                        principalTable: "chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_messages_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    thumbnail_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_attachments_chat_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_reactions",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_reactions", x => new { x.message_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_chat_reactions_chat_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_reactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_attachments_chat_id",
                table: "chat_attachments",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_attachments_message_id",
                table: "chat_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_members_user_id",
                table: "chat_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_author_id",
                table: "chat_messages",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_chat_id_author_id_client_id",
                table: "chat_messages",
                columns: new[] { "chat_id", "author_id", "client_id" },
                unique: true,
                filter: "client_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_chat_id_sequence",
                table: "chat_messages",
                columns: new[] { "chat_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_reactions_user_id",
                table: "chat_reactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_chats_direct_key",
                table: "chats",
                column: "direct_key",
                unique: true,
                filter: "direct_key IS NOT NULL");

            // Chats are part of every staff role from now on: grant the new permission to the predefined roles that
            // already exist (the administrator gets every new permission at startup; external users stay without it).
            migrationBuilder.Sql(
                """
                INSERT INTO role_claims (role_id, claim_type, claim_value)
                SELECT r.id, 'kod:permission', 'chats.use'
                FROM roles r
                WHERE r.code IN ('head', 'deputy-head', 'head-of-unit', 'deputy-head-of-unit', 'records-clerk', 'hr-specialist', 'communications', 'employee')
                  AND NOT EXISTS (
                      SELECT 1 FROM role_claims c
                      WHERE c.role_id = r.id AND c.claim_type = 'kod:permission' AND c.claim_value = 'chats.use');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_attachments");

            migrationBuilder.DropTable(
                name: "chat_members");

            migrationBuilder.DropTable(
                name: "chat_reactions");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chats");
        }
    }
}
