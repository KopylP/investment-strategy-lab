using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Identity.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.IntegrationTests;

internal static class NoStrategyEngine
{
    extension(IServiceCollection services)
    {
        public void RemoveReactionsThatNeedTheStrategyEngine() =>
            services.RemoveAll<IIntegrationEventHandler<UserRegisteredV1>>();
    }
}
