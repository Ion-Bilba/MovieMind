using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieMind.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferenceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferenceProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferenceProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferenceProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGenrePreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GenreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGenrePreferences", x => new { x.UserId, x.GenreId });
                    table.CheckConstraint("CK_UserGenrePreferences_Score", "[Score] >= 0 AND [Score] <= 10");
                    table.ForeignKey(
                        name: "FK_UserGenrePreferences_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGenrePreferences_UserPreferenceProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserPreferenceProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGenrePreferences_GenreId",
                table: "UserGenrePreferences",
                column: "GenreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGenrePreferences");

            migrationBuilder.DropTable(
                name: "UserPreferenceProfiles");
        }
    }
}
