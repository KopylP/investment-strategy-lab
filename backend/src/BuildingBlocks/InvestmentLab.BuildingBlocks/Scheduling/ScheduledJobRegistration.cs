using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Scheduling;

public sealed record ScheduledJobRegistration(Type JobType, IJobSchedule Schedule, bool RunAtStartup = false);
