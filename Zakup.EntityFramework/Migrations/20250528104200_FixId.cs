using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zakup.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FixId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdPostFiles_AdPostId",
                table: "AdPostFiles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdPostFiles",
                table: "AdPostFiles",
                columns: new[] { "AdPostId", "FileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AdPostFiles",
                table: "AdPostFiles");

            migrationBuilder.CreateIndex(
                name: "IX_AdPostFiles_AdPostId",
                table: "AdPostFiles",
                column: "AdPostId");
        }
    }
}
