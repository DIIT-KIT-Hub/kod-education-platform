using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KOD.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThreadedNewsComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "depth",
                table: "news_comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "root_id",
                table: "news_comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing comments had one level of replies: a reply's thread is its parent.
            migrationBuilder.Sql(
                """
                UPDATE news_comments
                SET root_id = COALESCE(parent_id, id),
                    depth = CASE WHEN parent_id IS NULL THEN 0 ELSE 1 END;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_news_comments_article_id_root_id",
                table: "news_comments",
                columns: new[] { "article_id", "root_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_news_comments_article_id_root_id",
                table: "news_comments");

            migrationBuilder.DropColumn(
                name: "depth",
                table: "news_comments");

            migrationBuilder.DropColumn(
                name: "root_id",
                table: "news_comments");
        }
    }
}
