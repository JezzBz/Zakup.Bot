using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Zakup.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelRatings",
                columns: table => new
                {
                    ChannelId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BadDeals = table.Column<long>(type: "bigint", nullable: false),
                    Rate = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelRatings", x => x.ChannelId);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MinutesToAcceptRequest = table.Column<long>(type: "bigint", nullable: true),
                    Alias = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ChannelChatId = table.Column<long>(type: "bigint", nullable: true),
                    HasDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageForwards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ForwardAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageForwards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    ThumbnailId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelJoinRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    InviteLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ApprovedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeclinedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelJoinRequests_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelegramAdPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Text = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Entities = table.Column<string>(type: "text", nullable: false),
                    Buttons = table.Column<string>(type: "text", nullable: false),
                    HasDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramAdPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramAdPosts_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramAdPosts_TelegramDocuments_FileId",
                        column: x => x.FileId,
                        principalTable: "TelegramDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TelegramZakups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPad = table.Column<bool>(type: "boolean", nullable: false),
                    InviteLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Platform = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false),
                    NeedApprove = table.Column<bool>(type: "boolean", nullable: false),
                    AdPostId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    ZakupSource = table.Column<string>(type: "text", nullable: false),
                    Admin = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    HasDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramZakups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramZakups_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramZakups_TelegramAdPosts_AdPostId",
                        column: x => x.AdPostId,
                        principalTable: "TelegramAdPosts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChannelMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: true),
                    IsCommenter = table.Column<bool>(type: "boolean", nullable: true),
                    UserName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    InviteLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    InviteLinkName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ZakupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Refer = table.Column<string>(type: "text", nullable: false),
                    JoinCount = table.Column<int>(type: "integer", nullable: false),
                    JoinedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeftUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TelegramZakupId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelMembers_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelMembers_TelegramZakups_TelegramZakupId",
                        column: x => x.TelegramZakupId,
                        principalTable: "TelegramZakups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelMembers_TelegramZakups_ZakupId",
                        column: x => x.ZakupId,
                        principalTable: "TelegramZakups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ZakupClients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ZakupId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZakupClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZakupClients_ChannelMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "ChannelMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZakupClients_TelegramZakups_ZakupId",
                        column: x => x.ZakupId,
                        principalTable: "TelegramZakups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelAdministrators",
                columns: table => new
                {
                    UsersId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    IsManual = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelAdministrators", x => new { x.ChannelId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_ChannelAdministrators_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelFeedback",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromUserId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    Positive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelFeedback", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpreadSheetId = table.Column<string>(type: "text", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelSheets_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpreadSheets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpreadSheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Refer = table.Column<string>(type: "text", nullable: true),
                    MutedToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserStateId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    CachedValue = table.Column<string>(type: "text", nullable: true),
                    PreviousMessageId = table.Column<int>(type: "integer", nullable: false),
                    MenuMessageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAdministrators_UsersId",
                table: "ChannelAdministrators",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelFeedback_FromUserId",
                table: "ChannelFeedback",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelJoinRequests_ChannelId",
                table: "ChannelJoinRequests",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_ChannelId",
                table: "ChannelMembers",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_TelegramZakupId",
                table: "ChannelMembers",
                column: "TelegramZakupId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_ZakupId",
                table: "ChannelMembers",
                column: "ZakupId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelSheets_ChannelId",
                table: "ChannelSheets",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelSheets_SpreadSheetId",
                table: "ChannelSheets",
                column: "SpreadSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_SpreadSheets_UserId",
                table: "SpreadSheets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAdPosts_ChannelId",
                table: "TelegramAdPosts",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAdPosts_FileId",
                table: "TelegramAdPosts",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramZakups_AdPostId",
                table: "TelegramZakups",
                column: "AdPostId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramZakups_ChannelId",
                table: "TelegramZakups",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserStateId",
                table: "Users",
                column: "UserStateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStates_UserId",
                table: "UserStates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZakupClients_MemberId",
                table: "ZakupClients",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ZakupClients_ZakupId",
                table: "ZakupClients",
                column: "ZakupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelAdministrators_Users_UsersId",
                table: "ChannelAdministrators",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelFeedback_Users_FromUserId",
                table: "ChannelFeedback",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelSheets_SpreadSheets_SpreadSheetId",
                table: "ChannelSheets",
                column: "SpreadSheetId",
                principalTable: "SpreadSheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SpreadSheets_Users_UserId",
                table: "SpreadSheets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserStates_UserStateId",
                table: "Users",
                column: "UserStateId",
                principalTable: "UserStates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserStates_Users_UserId",
                table: "UserStates");

            migrationBuilder.DropTable(
                name: "ChannelAdministrators");

            migrationBuilder.DropTable(
                name: "ChannelFeedback");

            migrationBuilder.DropTable(
                name: "ChannelJoinRequests");

            migrationBuilder.DropTable(
                name: "ChannelRatings");

            migrationBuilder.DropTable(
                name: "ChannelSheets");

            migrationBuilder.DropTable(
                name: "MessageForwards");

            migrationBuilder.DropTable(
                name: "ZakupClients");

            migrationBuilder.DropTable(
                name: "SpreadSheets");

            migrationBuilder.DropTable(
                name: "ChannelMembers");

            migrationBuilder.DropTable(
                name: "TelegramZakups");

            migrationBuilder.DropTable(
                name: "TelegramAdPosts");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "TelegramDocuments");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserStates");
        }
    }
}
