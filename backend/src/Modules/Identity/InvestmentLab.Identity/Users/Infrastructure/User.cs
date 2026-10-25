using Microsoft.AspNetCore.Identity;

namespace InvestmentLab.Identity.Users.Infrastructure;

/// <summary>Login identity; the id is the <see cref="UserId"/> every other module uses as owner.</summary>
public sealed class User : IdentityUser<Guid>;
