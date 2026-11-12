using InvestmentLab.Identity.Profiles.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Identity.Profiles.Infrastructure;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> profile)
    {
        profile.ToTable("user_profiles");
        profile.HasKey(p => p.Id);
        profile.Property(p => p.TimeZone).HasMaxLength(64);
        profile.Property(p => p.ChartStyle).HasConversion<string>().HasMaxLength(16);
    }
}
