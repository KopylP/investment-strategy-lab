using InvestmentLab.Backtesting;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.Identity;
using InvestmentLab.MarketData;
using InvestmentLab.Notifications;
using InvestmentLab.Performance;
using InvestmentLab.Portfolio;
using InvestmentLab.Rebalancing;
using InvestmentLab.Signals;
using InvestmentLab.Strategies;
using InvestmentLab.StrategyBuilder;
using InvestmentLab.Taxation;

namespace InvestmentLab.Api;

internal static class Modules
{
    public static IModule[] All() =>
    [
        new IdentityModule(),
        new MarketDataModule(),
        new StrategiesModule(),
        new PortfolioModule(),
        new RebalancingModule(),
        new TaxationModule(),
        new PerformanceModule(),
        new SignalsModule(),
        new BacktestingModule(),
        new StrategyBuilderModule(),
        new NotificationsModule(),
    ];
}
