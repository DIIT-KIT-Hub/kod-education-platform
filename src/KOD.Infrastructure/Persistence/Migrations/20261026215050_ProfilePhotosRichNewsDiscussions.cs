using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOD.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProfilePhotosRichNewsDiscussions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "photo_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "news_articles",
                type: "character varying(200000)",
                maxLength: 200000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50000)",
                oldMaxLength: 50000);

            migrationBuilder.AddColumn<bool>(
                name: "allow_comments",
                table: "news_articles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Articles written before the rich text editor are Markdown.
            migrationBuilder.AddColumn<string>(
                name: "body_format",
                table: "news_articles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Markdown");

            migrationBuilder.CreateTable(
                name: "media_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    news_article_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_files_news_articles_news_article_id",
                        column: x => x.news_article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "news_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.ForeignKey(
                        name: "fk_news_comments_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "fk_news_views_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
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
                        name: "fk_news_reactions_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
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
                name: "ix_users_photo_id",
                table: "users",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_files_news_article_id",
                table: "media_files",
                column: "news_article_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_files_purpose_created_at",
                table: "media_files",
                columns: new[] { "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_news_comments_article_id_parent_id_created_at",
                table: "news_comments",
                columns: new[] { "article_id", "parent_id", "created_at" });

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

            migrationBuilder.AddForeignKey(
                name: "fk_users_media_files_photo_id",
                table: "users",
                column: "photo_id",
                principalTable: "media_files",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_media_files_photo_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "media_files");

            migrationBuilder.DropTable(
                name: "news_reactions");

            migrationBuilder.DropTable(
                name: "news_views");

            migrationBuilder.DropTable(
                name: "news_comments");

            migrationBuilder.DropIndex(
                name: "ix_users_photo_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "photo_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "allow_comments",
                table: "news_articles");

            migrationBuilder.DropColumn(
                name: "body_format",
                table: "news_articles");

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "news_articles",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200000)",
                oldMaxLength: 200000);
        }
    }
}
