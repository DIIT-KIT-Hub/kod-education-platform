using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ODC.Web.Infrastructure.Persistence.Migrations.Organization
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateSequence(
                name: "document_number_seq");

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    details = table.Column<string>(type: "citext", maxLength: 2000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                });

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
                name: "members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", maxLength: 256, nullable: false),
                    last_name = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    first_name = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    middle_name = table.Column<string>(type: "citext", maxLength: 64, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    awaits_activation = table.Column<bool>(type: "boolean", nullable: false),
                    last_sign_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 256, nullable: false),
                    short_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_units_organization_units_parent_id",
                        column: x => x.parent_id,
                        principalTable: "organization_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_members",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_read_sequence = table.Column<long>(type: "bigint", nullable: false),
                    cleared_sequence = table.Column<long>(type: "bigint", nullable: false)
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
                        name: "fk_chat_members_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
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
                    forwarded_from_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "fk_chat_messages_members_author_id",
                        column: x => x.author_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chat_messages_members_forwarded_from_id",
                        column: x => x.forwarded_from_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    preview = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_members_actor_id",
                        column: x => x.actor_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permission_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    claim_value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_permission_overrides_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "citext", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    signing_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    signing_turn_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signing_turn_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    file_content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_members_author_id",
                        column: x => x.author_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documents_organization_units_organization_unit_id",
                        column: x => x.organization_unit_id,
                        principalTable: "organization_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "news_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "citext", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                    body_format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    allow_comments = table.Column<bool>(type: "boolean", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_news_articles_members_author_id",
                        column: x => x.author_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_news_articles_organization_units_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_positions_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_staff_positions_organization_units_organization_unit_id",
                        column: x => x.organization_unit_id,
                        principalTable: "organization_units",
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
                        name: "fk_chat_hidden_messages_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
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
                        name: "fk_chat_reactions_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_signatures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_signature_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    packaging = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    key_media = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cert_issuer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    cert_serial = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    cert_subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    cert_organization = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    cert_tax_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    cert_valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cert_valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    file_content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_signatures", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_signatures_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_signatures_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_signers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_signers", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_signers_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_signers_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "news_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    root_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_news_comments_members_author_id",
                        column: x => x.author_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_news_comments_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_news_comments_news_comments_parent_id",
                        column: x => x.parent_id,
                        principalTable: "news_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "news_views",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_views", x => new { x.article_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_news_views_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_news_views_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "news_reactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reacted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_reactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_news_reactions_members_user_id",
                        column: x => x.user_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_news_reactions_news_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_news_reactions_news_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "news_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_action",
                table: "audit_entries",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_occurred_at",
                table: "audit_entries",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_user_id",
                table: "audit_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_attachments_chat_id",
                table: "chat_attachments",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_attachments_message_id",
                table: "chat_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_hidden_messages_chat_id",
                table: "chat_hidden_messages",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_hidden_messages_user_id_chat_id",
                table: "chat_hidden_messages",
                columns: new[] { "user_id", "chat_id" });

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
                name: "ix_chat_messages_forwarded_from_id",
                table: "chat_messages",
                column: "forwarded_from_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_document_signatures_document_id",
                table: "document_signatures",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_signatures_user_id",
                table: "document_signatures",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_signers_document_id_order",
                table: "document_signers",
                columns: new[] { "document_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_signers_user_id_status",
                table: "document_signers",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_author_id",
                table: "documents",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_organization_unit_id",
                table: "documents",
                column: "organization_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_registration_number",
                table: "documents",
                column: "registration_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_status_created_at",
                table: "documents",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_members_email",
                table: "members",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_members_last_name_first_name",
                table: "members",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_author_id",
                table: "news_articles",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_is_pinned_published_at",
                table: "news_articles",
                columns: new[] { "is_pinned", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_organization_id",
                table: "news_articles",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_slug",
                table: "news_articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_news_comments_article_id_parent_id_created_at",
                table: "news_comments",
                columns: new[] { "article_id", "parent_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_news_comments_article_id_root_id",
                table: "news_comments",
                columns: new[] { "article_id", "root_id" });

            migrationBuilder.CreateIndex(
                name: "ix_news_comments_author_id",
                table: "news_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_comments_parent_id",
                table: "news_comments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_reactions_article_id",
                table: "news_reactions",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_reactions_article_reader",
                table: "news_reactions",
                columns: new[] { "article_id", "user_id" },
                unique: true,
                filter: "comment_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_news_reactions_comment_reader",
                table: "news_reactions",
                columns: new[] { "comment_id", "user_id" },
                unique: true,
                filter: "comment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_news_reactions_user_id",
                table: "news_reactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_views_user_id",
                table: "news_views",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_actor_id",
                table: "notifications",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_created_at",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_entity_id",
                table: "notifications",
                columns: new[] { "user_id", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_unread",
                table: "notifications",
                column: "user_id",
                filter: "read_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_code",
                table: "organization_units",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_parent_id",
                table: "organization_units",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_type_id",
                table: "organization_units",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_permission_overrides_user_id_claim_type_claim_value",
                table: "permission_overrides",
                columns: new[] { "user_id", "claim_type", "claim_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_positions_organization_unit_id",
                table: "staff_positions",
                column: "organization_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_positions_role_id",
                table: "staff_positions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_positions_user_id_organization_unit_id_role_id",
                table: "staff_positions",
                columns: new[] { "user_id", "organization_unit_id", "role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "chat_attachments");

            migrationBuilder.DropTable(
                name: "chat_hidden_messages");

            migrationBuilder.DropTable(
                name: "chat_members");

            migrationBuilder.DropTable(
                name: "chat_reactions");

            migrationBuilder.DropTable(
                name: "document_signatures");

            migrationBuilder.DropTable(
                name: "document_signers");

            migrationBuilder.DropTable(
                name: "news_reactions");

            migrationBuilder.DropTable(
                name: "news_views");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "permission_overrides");

            migrationBuilder.DropTable(
                name: "staff_positions");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "news_comments");

            migrationBuilder.DropTable(
                name: "chats");

            migrationBuilder.DropTable(
                name: "news_articles");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropTable(
                name: "organization_units");

            migrationBuilder.DropSequence(
                name: "document_number_seq");
        }
    }
}
