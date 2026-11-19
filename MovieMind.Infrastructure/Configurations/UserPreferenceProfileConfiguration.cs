using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieMind.Domain.Entities;

namespace MovieMind.Infrastructure.Configurations;

public sealed class UserPreferenceProfileConfiguration : IEntityTypeConfiguration<UserPreferenceProfile>
{
    public void Configure(EntityTypeBuilder<UserPreferenceProfile> builder)
    {
        builder.ToTable("UserPreferenceProfiles");

        builder.HasKey(profile => profile.UserId);

        builder.Property(profile => profile.UpdatedAt)
            .IsRequired();

        builder.HasOne(profile => profile.User)
            .WithOne(user => user.PreferenceProfile)
            .HasForeignKey<UserPreferenceProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.GenrePreferences)
            .WithOne(preference => preference.Profile)
            .HasForeignKey(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}