using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieMind.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RatingScoreTenPointScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Ratings_Score",
                table: "Ratings");

            migrationBuilder.AlterColumn<double>(
                name: "Score",
                table: "Ratings",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ratings_Score",
                table: "Ratings",
                sql: "[Score] >= 0 AND [Score] <= 10");

            migrationBuilder.Sql("UPDATE Ratings SET Score = Score * 2.0;");
            migrationBuilder.Sql("UPDATE Movies SET AverageRating = AverageRating * 2.0 WHERE RatingCount > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Ratings_Score",
                table: "Ratings");

            migrationBuilder.Sql("UPDATE Ratings SET Score = CASE WHEN Score <= 0 THEN 1 WHEN Score >= 10 THEN 5 ELSE ROUND(Score / 2.0, 0) END;");
            migrationBuilder.Sql("UPDATE Movies SET AverageRating = AverageRating / 2.0 WHERE RatingCount > 0;");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "Ratings",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ratings_Score",
                table: "Ratings",
                sql: "[Score] >= 1 AND [Score] <= 5");
        }
    }
}
