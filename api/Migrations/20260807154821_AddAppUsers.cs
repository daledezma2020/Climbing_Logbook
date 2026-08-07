using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LogEntries.UserId is non-nullable and there is no user to attribute pre-existing rows to.
            // The local dev data was throwaway seed/test entries, so they are dropped rather than backfilled.
            migrationBuilder.Sql("DELETE FROM \"LogEntries\";");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "Comments");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "LogEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Auth0Subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PictureUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HomePlaceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUsers_Places_HomePlaceId",
                        column: x => x.HomePlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_UserId",
                table: "LogEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs",
                sql: "(\n    \"PlaceId\" IS NOT NULL\n    AND \"BoardConfigurationId\" IS NULL\n    AND \"CustomLocationName\" IS NULL\n    AND \"CustomLocationLatitude\" IS NULL\n    AND \"CustomLocationLongitude\" IS NULL\n)\nOR (\n    \"PlaceId\" IS NULL\n    AND \"BoardConfigurationId\" IS NOT NULL\n    AND \"CustomLocationName\" IS NULL\n    AND \"CustomLocationLatitude\" IS NULL\n    AND \"CustomLocationLongitude\" IS NULL\n)\nOR (\n    \"PlaceId\" IS NULL\n    AND \"BoardConfigurationId\" IS NULL\n    AND \"CustomLocationName\" IS NOT NULL\n    AND \"CustomLocationLatitude\" IS NOT NULL\n    AND \"CustomLocationLongitude\" IS NOT NULL\n)");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Auth0Subject",
                table: "AppUsers",
                column: "Auth0Subject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_HomePlaceId",
                table: "AppUsers",
                column: "HomePlaceId");

            // EF Core cannot express a functional index, so the case-insensitive username
            // uniqueness guarantee is created directly.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_AppUsers_Username_Lower\" ON \"AppUsers\" (LOWER(\"Username\"));");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AppUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogEntries_AppUsers_UserId",
                table: "LogEntries",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_AppUsers_Username_Lower\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AppUsers_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_LogEntries_AppUsers_UserId",
                table: "LogEntries");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_LogEntries_UserId",
                table: "LogEntries");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserId",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "Comments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs",
                sql: "\r\n(\r\n    \"PlaceId\" IS NOT NULL\r\n    AND \"BoardConfigurationId\" IS NULL\r\n    AND \"CustomLocationName\" IS NULL\r\n    AND \"CustomLocationLatitude\" IS NULL\r\n    AND \"CustomLocationLongitude\" IS NULL\r\n)\r\nOR (\r\n    \"PlaceId\" IS NULL\r\n    AND \"BoardConfigurationId\" IS NOT NULL\r\n    AND \"CustomLocationName\" IS NULL\r\n    AND \"CustomLocationLatitude\" IS NULL\r\n    AND \"CustomLocationLongitude\" IS NULL\r\n)\r\nOR (\r\n    \"PlaceId\" IS NULL\r\n    AND \"BoardConfigurationId\" IS NULL\r\n    AND \"CustomLocationName\" IS NOT NULL\r\n    AND \"CustomLocationLatitude\" IS NOT NULL\r\n    AND \"CustomLocationLongitude\" IS NOT NULL\r\n)");
        }
    }
}
