using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Identity.Shared.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "identity";

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override string Schema => SchemaName;

    protected override Assembly MappingsAssembly => typeof(IdentityAssemblyMarker).Assembly;
}
