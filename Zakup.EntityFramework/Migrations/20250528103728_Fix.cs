using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zakup.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdPostFiles_TelegramDocuments_FileId1",
                table: "AdPostFiles");

            migrationBuilder.DropIndex(
                name: "IX_AdPostFiles_FileId1",
                table: "AdPostFiles");

            migrationBuilder.DropColumn(
                name: "FileId1",
                table: "AdPostFiles");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileId",
                table: "AdPostFiles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_AdPostFiles_FileId",
                table: "AdPostFiles",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdPostFiles_TelegramDocuments_FileId",
                table: "AdPostFiles",
                column: "FileId",
                principalTable: "TelegramDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdPostFiles_TelegramDocuments_FileId",
                table: "AdPostFiles");

            migrationBuilder.DropIndex(
                name: "IX_AdPostFiles_FileId",
                table: "AdPostFiles");

            migrationBuilder.AlterColumn<string>(
                name: "FileId",
                table: "AdPostFiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "FileId1",
                table: "AdPostFiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AdPostFiles_FileId1",
                table: "AdPostFiles",
                column: "FileId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AdPostFiles_TelegramDocuments_FileId1",
                table: "AdPostFiles",
                column: "FileId1",
                principalTable: "TelegramDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
