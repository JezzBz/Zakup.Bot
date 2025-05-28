using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zakup.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaGroupEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdPostFiles");

            migrationBuilder.DropColumn(
                name: "MediaGroupId",
                table: "TelegramDocuments");

            migrationBuilder.AddColumn<string>(
                name: "MediaGroupId",
                table: "TelegramAdPosts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MediaGroups",
                columns: table => new
                {
                    MediaGroupId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaGroups", x => x.MediaGroupId);
                });

            migrationBuilder.CreateTable(
                name: "FileMediaGroups",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaGroupId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMediaGroups", x => new { x.FileId, x.MediaGroupId });
                    table.ForeignKey(
                        name: "FK_FileMediaGroups_MediaGroups_MediaGroupId",
                        column: x => x.MediaGroupId,
                        principalTable: "MediaGroups",
                        principalColumn: "MediaGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileMediaGroups_TelegramDocuments_FileId",
                        column: x => x.FileId,
                        principalTable: "TelegramDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAdPosts_MediaGroupId",
                table: "TelegramAdPosts",
                column: "MediaGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMediaGroups_MediaGroupId",
                table: "FileMediaGroups",
                column: "MediaGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramAdPosts_MediaGroups_MediaGroupId",
                table: "TelegramAdPosts",
                column: "MediaGroupId",
                principalTable: "MediaGroups",
                principalColumn: "MediaGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelegramAdPosts_MediaGroups_MediaGroupId",
                table: "TelegramAdPosts");

            migrationBuilder.DropTable(
                name: "FileMediaGroups");

            migrationBuilder.DropTable(
                name: "MediaGroups");

            migrationBuilder.DropIndex(
                name: "IX_TelegramAdPosts_MediaGroupId",
                table: "TelegramAdPosts");

            migrationBuilder.DropColumn(
                name: "MediaGroupId",
                table: "TelegramAdPosts");

            migrationBuilder.AddColumn<string>(
                name: "MediaGroupId",
                table: "TelegramDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AdPostFiles",
                columns: table => new
                {
                    AdPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdPostFiles", x => new { x.AdPostId, x.FileId });
                    table.ForeignKey(
                        name: "FK_AdPostFiles_TelegramAdPosts_AdPostId",
                        column: x => x.AdPostId,
                        principalTable: "TelegramAdPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdPostFiles_TelegramDocuments_FileId",
                        column: x => x.FileId,
                        principalTable: "TelegramDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdPostFiles_FileId",
                table: "AdPostFiles",
                column: "FileId");
        }
    }
}
