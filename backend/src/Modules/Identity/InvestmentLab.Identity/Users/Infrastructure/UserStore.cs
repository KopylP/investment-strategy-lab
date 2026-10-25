using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using IdentityDbContext = InvestmentLab.Identity.Shared.Infrastructure.IdentityDbContext;

namespace InvestmentLab.Identity.Users.Infrastructure;

/// <summary>EF store over the module's own context (not IdentityDbContext&lt;T&gt;, which would bring its own model and no outbox).</summary>
internal sealed class UserStore(IdentityDbContext context, IdentityErrorDescriber? describer = null)
    : UserOnlyStore<User, IdentityDbContext, Guid>(context, describer);
