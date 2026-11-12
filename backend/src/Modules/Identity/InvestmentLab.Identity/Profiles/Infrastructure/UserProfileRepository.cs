using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Identity.Profiles.Infrastructure;

internal sealed class UserProfileRepository(IdentityDbContext db) : IUserProfileRepository
{
    public Task<UserProfile?> FindAsync(UserId userId, CancellationToken cancellationToken) =>
        db.UserProfiles.SingleOrDefaultAsync(profile => profile.Id == userId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
