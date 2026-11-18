using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieMind.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RatingScoreTenPointScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = 0;", suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE TABLE "Ratings_new" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Ratings" PRIMARY KEY,
                    "UserId" TEXT NOT NULL,
                    "MovieId" TEXT NOT NULL,
                    "Score" REAL NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NULL,
                    CONSTRAINT "CK_Ratings_Score" CHECK ("Score" >= 0 AND "Score" <= 10),
                    CONSTRAINT "FK_Ratings_Movies_MovieId" FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_Ratings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "Ratings_new" ("Id", "UserId", "MovieId", "Score", "CreatedAt", "UpdatedAt")
                SELECT "Id", "UserId", "MovieId", "Score" * 2.0, "CreatedAt", "UpdatedAt"
                FROM "Ratings";

                DROP TABLE "Ratings";
                ALTER TABLE "Ratings_new" RENAME TO "Ratings";

                CREATE INDEX "IX_Ratings_MovieId" ON "Ratings" ("MovieId");
                CREATE UNIQUE INDEX "IX_Ratings_UserId_MovieId" ON "Ratings" ("UserId", "MovieId");

                UPDATE "Movies"
                SET "AverageRating" = "AverageRating" * 2.0
                WHERE "RatingCount" > 0;
                """);

            migrationBuilder.Sql("PRAGMA foreign_keys = 1;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = 0;", suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE TABLE "Ratings_old_scale" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Ratings" PRIMARY KEY,
                    "UserId" TEXT NOT NULL,
                    "MovieId" TEXT NOT NULL,
                    "Score" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NULL,
                    CONSTRAINT "CK_Ratings_Score" CHECK ("Score" >= 1 AND "Score" <= 5),
                    CONSTRAINT "FK_Ratings_Movies_MovieId" FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_Ratings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "Ratings_old_scale" ("Id", "UserId", "MovieId", "Score", "CreatedAt", "UpdatedAt")
                SELECT "Id", "UserId", "MovieId",
                    CASE
                        WHEN "Score" <= 0 THEN 1
                        WHEN "Score" >= 10 THEN 5
                        ELSE CAST(ROUND("Score" / 2.0, 0) AS INTEGER)
                    END,
                    "CreatedAt", "UpdatedAt"
                FROM "Ratings";

                DROP TABLE "Ratings";
                ALTER TABLE "Ratings_old_scale" RENAME TO "Ratings";

                CREATE INDEX "IX_Ratings_MovieId" ON "Ratings" ("MovieId");
                CREATE UNIQUE INDEX "IX_Ratings_UserId_MovieId" ON "Ratings" ("UserId", "MovieId");

                UPDATE "Movies"
                SET "AverageRating" = "AverageRating" / 2.0
                WHERE "RatingCount" > 0;
                """);

            migrationBuilder.Sql("PRAGMA foreign_keys = 1;", suppressTransaction: true);
        }
    }
}
