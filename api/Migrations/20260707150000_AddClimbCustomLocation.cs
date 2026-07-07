using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260707150000_AddClimbCustomLocation")]
    public partial class AddClimbCustomLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs");

            migrationBuilder.AddColumn<double>(
                name: "CustomLocationLatitude",
                table: "Climbs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CustomLocationLongitude",
                table: "Climbs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomLocationName",
                table: "Climbs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs",
                sql: """
                     (
                         "PlaceId" IS NOT NULL
                         AND "BoardConfigurationId" IS NULL
                         AND "CustomLocationName" IS NULL
                         AND "CustomLocationLatitude" IS NULL
                         AND "CustomLocationLongitude" IS NULL
                     )
                     OR (
                         "PlaceId" IS NULL
                         AND "BoardConfigurationId" IS NOT NULL
                         AND "CustomLocationName" IS NULL
                         AND "CustomLocationLatitude" IS NULL
                         AND "CustomLocationLongitude" IS NULL
                     )
                     OR (
                         "PlaceId" IS NULL
                         AND "BoardConfigurationId" IS NULL
                         AND "CustomLocationName" IS NOT NULL
                         AND "CustomLocationLatitude" IS NOT NULL
                         AND "CustomLocationLongitude" IS NOT NULL
                     )
                     """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs");

            migrationBuilder.DropColumn(
                name: "CustomLocationLatitude",
                table: "Climbs");

            migrationBuilder.DropColumn(
                name: "CustomLocationLongitude",
                table: "Climbs");

            migrationBuilder.DropColumn(
                name: "CustomLocationName",
                table: "Climbs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Climbs_Context",
                table: "Climbs",
                sql: "(\"PlaceId\" IS NOT NULL AND \"BoardConfigurationId\" IS NULL) OR (\"PlaceId\" IS NULL AND \"BoardConfigurationId\" IS NOT NULL)");
        }
    }
}
