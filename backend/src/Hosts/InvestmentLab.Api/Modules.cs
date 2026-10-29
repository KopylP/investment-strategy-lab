using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.Identity;
using InvestmentLab.MarketData;
using InvestmentLab.Notifications;
using InvestmentLab.Performance;
using InvestmentLab.Portfolio;
using InvestmentLab.Rebalancing;
using InvestmentLab.Strategies;
using InvestmentLab.Taxation;

namespace InvestmentLab.Api;

/// <summary>Composition root: the only place that knows which modules make up the monolith.</summary>
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
        new NotificationsModule(),
    ];
}
