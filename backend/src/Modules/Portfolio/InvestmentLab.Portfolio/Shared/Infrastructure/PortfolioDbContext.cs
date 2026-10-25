using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Accounts.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Domain;
using InvestmentLab.Portfolio.Transactions.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Portfolio.Shared.Infrastructure;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "portfolio";

    public DbSet<InvestmentAccount> Accounts => Set<InvestmentAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<HoldingRow> HoldingRows => Set<HoldingRow>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

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
