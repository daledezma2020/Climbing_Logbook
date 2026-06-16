using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ParentPlaceId = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Places_Places_ParentPlaceId",
                        column: x => x.ParentPlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "PlaceExternalReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaceId = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetrievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceExternalReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceExternalReferences_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Climbs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    GradeSystem = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlaceId = table.Column<int>(type: "integer", nullable: true),
                    BoardConfigurationId = table.Column<int>(type: "integer", nullable: true),
                    SetterId = table.Column<int>(type: "integer", nullable: true),
                    FirstAscentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PictureUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Climbs", x => x.Id);
                    table.CheckConstraint("CK_Climbs_Context", "(\"PlaceId\" IS NOT NULL AND \"BoardConfigurationId\" IS NULL) OR (\"PlaceId\" IS NULL AND \"BoardConfigurationId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Climbs_BoardConfigurations_BoardConfigurationId",
                        column: x => x.BoardConfigurationId,
                        principalTable: "BoardConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Climbs_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Climbs_Setters_SetterId",
                        column: x => x.SetterId,
                        principalTable: "Setters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClimbExternalReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClimbId = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetrievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClimbExternalReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClimbExternalReferences_Climbs_ClimbId",
                        column: x => x.ClimbId,
                        principalTable: "Climbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Author = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClimbId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Climbs_ClimbId",
                        column: x => x.ClimbId,
                        principalTable: "Climbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClimbId = table.Column<int>(type: "integer", nullable: false),
                    PlaceId = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_Climbs_ClimbId",
                        column: x => x.ClimbId,
                        principalTable: "Climbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogEntries_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClimbExternalReferences_ClimbId",
                table: "ClimbExternalReferences",
                column: "ClimbId");

            migrationBuilder.CreateIndex(
                name: "IX_ClimbExternalReferences_Provider_ExternalId",
                table: "ClimbExternalReferences",
                columns: new[] { "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Climbs_BoardConfigurationId",
                table: "Climbs",
                column: "BoardConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Climbs_PlaceId",
                table: "Climbs",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Climbs_SetterId",
                table: "Climbs",
                column: "SetterId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ClimbId",
                table: "Comments",
                column: "ClimbId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_ClimbId",
                table: "LogEntries",
                column: "ClimbId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_PlaceId",
                table: "LogEntries",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceExternalReferences_PlaceId",
                table: "PlaceExternalReferences",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceExternalReferences_Provider_ExternalId",
                table: "PlaceExternalReferences",
                columns: new[] { "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Places_ParentPlaceId",
                table: "Places",
                column: "ParentPlaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClimbExternalReferences");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "PlaceExternalReferences");

            migrationBuilder.DropTable(
                name: "Climbs");

            migrationBuilder.DropTable(
                name: "BoardConfigurations");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "Setters");
        }
    }
}
