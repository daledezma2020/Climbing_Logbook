using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationsAndSetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "ClimbRoutes");

            migrationBuilder.DropColumn(
                name: "Setter",
                table: "ClimbRoutes");

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "ClimbRoutes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SetterId",
                table: "ClimbRoutes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<float>(type: "real", nullable: false),
                    Longitude = table.Column<float>(type: "real", nullable: false),
                    Address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Setters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Setters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClimbRoutes_LocationId",
                table: "ClimbRoutes",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClimbRoutes_SetterId",
                table: "ClimbRoutes",
                column: "SetterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClimbRoutes_Locations_LocationId",
                table: "ClimbRoutes",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClimbRoutes_Setters_SetterId",
                table: "ClimbRoutes",
                column: "SetterId",
                principalTable: "Setters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClimbRoutes_Locations_LocationId",
                table: "ClimbRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_ClimbRoutes_Setters_SetterId",
                table: "ClimbRoutes");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Setters");

            migrationBuilder.DropIndex(
                name: "IX_ClimbRoutes_LocationId",
                table: "ClimbRoutes");

            migrationBuilder.DropIndex(
                name: "IX_ClimbRoutes_SetterId",
                table: "ClimbRoutes");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "ClimbRoutes");

            migrationBuilder.DropColumn(
                name: "SetterId",
                table: "ClimbRoutes");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "ClimbRoutes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Setter",
                table: "ClimbRoutes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
