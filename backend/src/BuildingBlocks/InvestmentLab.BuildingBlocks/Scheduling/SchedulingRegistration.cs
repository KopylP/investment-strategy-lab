using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Scheduling;

public static class SchedulingRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddScheduledJob<TJob>(IJobSchedule schedule, bool runAtStartup = false)
            where TJob : class, IScheduledJob
        {
            services.TryAddScoped<TJob>();
            services.AddSingleton(new ScheduledJobRegistration(typeof(TJob), schedule, runAtStartup));

            return services;
        }
    }
}
