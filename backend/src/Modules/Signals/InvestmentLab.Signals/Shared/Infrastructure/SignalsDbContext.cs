using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Signals.Monitoring.Domain;
using InvestmentLab.Signals.Monitoring.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Signals.Shared.Infrastructure;

public sealed class SignalsDbContext(DbContextOptions<SignalsDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "signals";

    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<SignalOccurrence> Occurrences => Set<SignalOccurrence>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SignalConfiguration(this));
        modelBuilder.ApplyConfiguration(new SignalOccurrenceConfiguration(this));
    }
}
