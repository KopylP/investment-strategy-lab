using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.StrategyBuilder.Drafts.Application.CleanUpTrials;
using InvestmentLab.StrategyBuilder.Drafts.Application.PublishDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.OpenPublishedStrategy;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetPublishedStrategy;
using InvestmentLab.StrategyBuilder.Trials.Api;
using InvestmentLab.StrategyBuilder.Trials.Application.ListTrials;
using InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;
using InvestmentLab.StrategyBuilder.Trials.Domain;
using InvestmentLab.StrategyBuilder.Trials.Infrastructure;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.StrategyBuilder.Drafts.Api;
using InvestmentLab.StrategyBuilder.Drafts.Application.ChangeDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.CheckDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.CreateDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.CopyEntryToExit;
using InvestmentLab.StrategyBuilder.Drafts.Application.DiscardDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetDraftRules;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetVocabulary;
using InvestmentLab.StrategyBuilder.Drafts.Application.ImportRules;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListDrafts;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Drafts.Application.MoveNodes;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Drafts.Infrastructure;
using InvestmentLab.StrategyBuilder.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.StrategyBuilder;

public sealed class StrategyBuilderModule : IModule
{
    public const string Schema = StrategyBuilderDbContext.SchemaName;

    public string Name => "StrategyBuilder";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<StrategyBuilderDbContext>(Schema);

        var services = builder.Services;
        services.AddSingleton<ITrialRunGate, TrialRunGate>();
        services.AddScoped<ITrialBacktests, TrialBacktests>();
        services.AddScoped<RunTrialHandler>();
        services.AddScoped<ListTrialsHandler>();
        services.AddScoped<IPublishedLayoutRepository, PublishedLayoutRepository>();
        services.AddScoped<ITrialCleanupRepository, TrialCleanupRepository>();
        services.AddScoped<CleanUpTrialHandler>();
        services.AddScoped<PublishDraftHandler>();
        services.AddScoped<OpenPublishedStrategyHandler>();
        services.AddScoped<GetPublishedStrategyHandler>();
        services.AddScheduledJob<TrialCleanupJob>(TrialCleanupJob.Schedule);
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IRuleVocabulary, RuleVocabularyFromSchema>();
        services.AddScoped<IInstrumentDirectory, InstrumentDirectory>();
        services.AddScoped<IStrategyAuthoring, StrategyAuthoring>();
        services.AddScoped<DraftDescriber>();
        services.AddScoped<DraftChecker>();
        services.AddScoped<GetVocabularyHandler>();
        services.AddScoped<ListInstrumentsHandler>();
        services.AddScoped<CreateDraftHandler>();
        services.AddScoped<CopyEntryToExitHandler>();
        services.AddScoped<GetDraftHandler>();
        services.AddScoped<ListDraftsHandler>();
        services.AddScoped<ChangeDraftHandler>();
        services.AddScoped<MoveNodesHandler>();
        services.AddScoped<CheckDraftHandler>();
        services.AddScoped<GetDraftRulesHandler>();
        services.AddScoped<ImportRulesHandler>();
        services.AddScoped<DiscardDraftHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/strategy-builder").MapDrafts().MapTrials();
}
