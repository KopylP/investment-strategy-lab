using System.Linq.Expressions;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Accounts.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Domain;
using InvestmentLab.Portfolio.Transactions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Portfolio.Shared.Infrastructure;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "portfolio";

    /// <summary>Named query filter: a signed-in user sees only own data; background work (no user) sees all.</summary>
    public const string OwnerFilter = "Owner";

    // A user id nobody has; stands in when there is no signed-in user so the filter stays one parameterised query.
    private static readonly UserId Nobody = UserId.Of(Guid.AllBitsSet);

    public DbSet<InvestmentAccount> Accounts => Set<InvestmentAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<HoldingRow> HoldingRows => Set<HoldingRow>();

    protected override string Schema => SchemaName;

    private bool HasCurrentUser => currentUser.Id is not null;
    private UserId CurrentOwner => currentUser.Id ?? Nobody;

    internal void HasOwnerFilter<TEntity>(EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, UserId>> owner)
        where TEntity : class
    {
        var row = owner.Parameters[0];
        var visible = Expression.OrElse(
            Expression.Not(Expression.Property(Expression.Constant(this), nameof(HasCurrentUser))),
            Expression.Equal(owner.Body, Expression.Property(Expression.Constant(this), nameof(CurrentOwner))));
        entity.HasQueryFilter(OwnerFilter, Expression.Lambda<Func<TEntity, bool>>(visible, row));
    }

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InvestmentAccountConfiguration(this));
        modelBuilder.ApplyConfiguration(new SubPortfolioConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration(this));
        modelBuilder.ApplyConfiguration(new HoldingRowConfiguration(this));
    }
}

internal static class PortfolioConverters
{
    public sealed class AccountIdConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<AccountId, Guid>(
        id => id.Value, value => AccountId.Of(value));

    public sealed class SubPortfolioIdConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<SubPortfolioId, Guid>(
        id => id.Value, value => SubPortfolioId.Of(value));

    public sealed class TargetShareConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TargetShare, decimal>(
        share => share.Value.Value, value => TargetShare.Of(Percentage.Of(value)));
}

internal sealed class PortfolioDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<PortfolioDbContext>
{
    protected override string Schema => PortfolioDbContext.SchemaName;

    protected override PortfolioDbContext Create(DbContextOptions<PortfolioDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
