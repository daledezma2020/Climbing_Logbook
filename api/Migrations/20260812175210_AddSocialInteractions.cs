using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ClimbId",
                table: "Comments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "LogEntryId",
                table: "Comments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LogEntryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Likes_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Likes_LogEntries_LogEntryId",
                        column: x => x.LogEntryId,
                        principalTable: "LogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_LogEntryId",
                table: "Comments",
                column: "LogEntryId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_Target",
                table: "Comments",
                sql: "(\n    \"ClimbId\" IS NOT NULL AND \"LogEntryId\" IS NULL\n)\nOR (\n    \"ClimbId\" IS NULL AND \"LogEntryId\" IS NOT NULL\n)");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_LogEntryId",
                table: "Likes",
                column: "LogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId_LogEntryId",
                table: "Likes",
                columns: new[] { "UserId", "LogEntryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_LogEntries_LogEntryId",
                table: "Comments",
                column: "LogEntryId",
                principalTable: "LogEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_LogEntries_LogEntryId",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropIndex(
                name: "IX_Comments_LogEntryId",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_Target",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "LogEntryId",
                table: "Comments");

            migrationBuilder.AlterColumn<int>(
                name: "ClimbId",
                table: "Comments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
