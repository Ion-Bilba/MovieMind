using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieMind.Domain.Entities;

namespace MovieMind.Infrastructure.Configurations;

public sealed class UserGenrePreferenceConfiguration : IEntityTypeConfiguration<UserGenrePreference>
{
    public void Configure(EntityTypeBuilder<UserGenrePreference> builder)
    {
        builder.ToTable("UserGenrePreferences", table =>
            table.HasCheckConstraint("CK_UserGenrePreferences_Score", "[Score] >= 0 AND [Score] <= 10"));

        builder.HasKey(preference => new { preference.UserId, preference.GenreId });

        builder.Property(preference => preference.Score)
            .IsRequired();

        builder.HasOne(preference => preference.Genre)
            .WithMany(genre => genre.UserPreferences)
            .HasForeignKey(preference => preference.GenreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}