using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zakup.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaGroupId",
                table: "TelegramDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaGroupId",
                table: "TelegramDocuments");
        }
    }
}
