using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal static class UserConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            user.HasIndex(u => u.NormalizedUserName).IsUnique();
            user.HasIndex(u => u.NormalizedEmail).IsUnique();
            user.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();
            user.Property(u => u.UserName).HasMaxLength(256);
            user.Property(u => u.NormalizedUserName).HasMaxLength(256);
            user.Property(u => u.Email).HasMaxLength(256);
            user.Property(u => u.NormalizedEmail).HasMaxLength(256);
            user.HasMany<IdentityUserClaim<Guid>>().WithOne().HasForeignKey(claim => claim.UserId).IsRequired();
            user.HasMany<IdentityUserLogin<Guid>>().WithOne().HasForeignKey(login => login.UserId).IsRequired();
            user.HasMany<IdentityUserToken<Guid>>().WithOne().HasForeignKey(token => token.UserId).IsRequired();
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(claim =>
        {
            claim.ToTable("user_claims");
            claim.HasKey(c => c.Id);
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(login =>
        {
            login.ToTable("user_logins");
            login.HasKey(l => new { l.LoginProvider, l.ProviderKey });
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(token =>
        {
            token.ToTable("user_tokens");
            token.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
        });
    }
}
