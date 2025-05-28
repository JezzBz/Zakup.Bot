using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zakup.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Files : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelegramAdPosts_TelegramDocuments_FileId",
                table: "TelegramAdPosts");

            migrationBuilder.DropIndex(
                name: "IX_TelegramAdPosts_FileId",
                table: "TelegramAdPosts");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "TelegramAdPosts");

            migrationBuilder.CreateTable(
                name: "AdPostFiles",
                columns: table => new
                {
                    FileId = table.Column<string>(type: "text", nullable: false),
                    FileId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    AdPostId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_AdPostFiles_TelegramAdPosts_AdPostId",
                        column: x => x.AdPostId,
                        principalTable: "TelegramAdPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdPostFiles_TelegramDocuments_FileId1",
                        column: x => x.FileId1,
                        principalTable: "TelegramDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdPostFiles_AdPostId",
                table: "AdPostFiles",
                column: "AdPostId");

            migrationBuilder.CreateIndex(
                name: "IX_AdPostFiles_FileId1",
                table: "AdPostFiles",
                column: "FileId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdPostFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "FileId",
                table: "TelegramAdPosts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAdPosts_FileId",
                table: "TelegramAdPosts",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramAdPosts_TelegramDocuments_FileId",
                table: "TelegramAdPosts",
                column: "FileId",
                principalTable: "TelegramDocuments",
                principalColumn: "Id");
        }
    }
}
