using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using IdentityDbContext = InvestmentLab.Identity.Shared.Infrastructure.IdentityDbContext;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class UserStore(IdentityDbContext context, IdentityErrorDescriber? describer = null)
    : UserOnlyStore<User, IdentityDbContext, Guid>(context, describer);
