using System.Text.Json;
using InvestmentLab.Backtesting.Replays.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal static class BacktestConfiguration
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Configure(EntityTypeBuilder<Backtest> backtest)
    {
        backtest.ToTable("backtests");
        backtest.HasKey(entry => entry.Id);
        backtest.Property(entry => entry.Id).ValueGeneratedNever();
        backtest.Property(entry => entry.OwnerId).HasConversion(id => id.Value, value => UserId.Of(value));
        backtest.Property(entry => entry.Status).HasConversion<string>().HasMaxLength(20);
        backtest.Property(entry => entry.FailureReason).HasMaxLength(2000);
        backtest.HasIndex(entry => new { entry.StrategyId, entry.StrategyVersion }).IsUnique();
        backtest.Ignore(entry => entry.IsWaitingOrRunning);

        backtest.Property(entry => entry.Result)
            .HasColumnType("jsonb")
            .HasConversion(
                result => result == null ? null : JsonSerializer.Serialize(result, Json),
                text => text == null ? null : JsonSerializer.Deserialize<ReplayResult>(text, Json),
                new ValueComparer<ReplayResult?>((left, right) => ReferenceEquals(left, right), result => result == null ? 0 : result.GetHashCode()));
    }
}
