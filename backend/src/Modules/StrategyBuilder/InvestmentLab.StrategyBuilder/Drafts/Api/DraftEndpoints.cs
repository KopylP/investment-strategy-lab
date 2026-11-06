using InvestmentLab.StrategyBuilder.Drafts.Application.PublishDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.OpenPublishedStrategy;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetPublishedStrategy;
using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
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
using InvestmentLab.StrategyBuilder.Drafts.Application.ListOwnSignals;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Drafts.Application.MoveNodes;
using InvestmentLab.StrategyBuilder.Drafts.Application.ReplaceDraftRules;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.StrategyBuilder.Drafts.Api;

internal static class DraftEndpoints
{
    public static RouteGroupBuilder MapDrafts(this RouteGroupBuilder group)
    {
        var builder = group.MapGroup("").WithTags("StrategyBuilder");

        builder.MapPost("/drafts/{draftId:guid}/publish", PublishDraft).WithName("PublishDraft");
        builder.MapPost("/published/{strategyId:guid}/versions/{version:int}/draft", OpenPublishedStrategy).WithName("OpenPublishedStrategy");
        builder.MapGet("/published/{strategyId:guid}/versions/{version:int}", GetPublishedStrategy).WithName("GetPublishedBuilderStrategy");
        builder.MapGet("/vocabulary", GetVocabulary).WithName("GetBuilderVocabulary");
        builder.MapGet("/signals", ListOwnSignals).WithName("ListBuilderSignals");
        builder.MapGet("/instruments", ListInstruments).WithName("ListBuilderInstruments");
        builder.MapGet("/drafts", ListDrafts).WithName("ListDrafts");
        builder.MapPost("/drafts", CreateDraft).WithName("CreateDraft");
        builder.MapPost("/drafts/import", ImportRules).WithName("ImportRules");
        builder.MapGet("/drafts/{draftId:guid}", GetDraft).WithName("GetDraft");
        builder.MapPut("/drafts/{draftId:guid}", ChangeDraft).WithName("ChangeDraft");
        builder.MapPut("/drafts/{draftId:guid}/layout", MoveNodes).WithName("MoveDraftNodes");
        builder.MapPost("/drafts/{draftId:guid}/check", CheckDraft).WithName("CheckDraft");
        builder.MapPost("/drafts/{draftId:guid}/copy-entry-to-exit", CopyEntryToExit).WithName("CopyEntryToExit");
        builder.MapGet("/drafts/{draftId:guid}/rules", GetDraftRules).WithName("GetDraftRules");
        builder.MapPut("/drafts/{draftId:guid}/rules", ReplaceDraftRules).WithName("ReplaceDraftRules");
        builder.MapDelete("/drafts/{draftId:guid}", DiscardDraft).WithName("DiscardDraft");

        return group;
    }

    private static async Task<IResult> PublishDraft(Guid draftId, PublishDraftCommand body, PublishDraftHandler handler, CancellationToken cancellationToken)
    {
        var published = await handler.Handle(draftId, body.Revision, cancellationToken);

        return published.ToHttpResult();
    }

    private static async Task<IResult> OpenPublishedStrategy(Guid strategyId, int version, OpenPublishedStrategyHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var draft = await handler.Handle(user.RequiredId, strategyId, version, cancellationToken);

        return draft.ToHttpResult();
    }

    private static async Task<IResult> GetPublishedStrategy(Guid strategyId, int version, GetPublishedStrategyHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var published = await handler.Handle(user.RequiredId, strategyId, version, cancellationToken);

        return published.ToHttpResult();
    }

    private static async Task<IResult> GetVocabulary(GetVocabularyHandler handler, CancellationToken cancellationToken)
    {
        var vocabulary = await handler.Handle(cancellationToken);

        return vocabulary.ToHttpResult();
    }

    private static async Task<IResult> ListOwnSignals(ListOwnSignalsHandler handler, CancellationToken cancellationToken)
    {
        var signals = await handler.Handle(cancellationToken);

        return TypedResults.Ok(signals);
    }

    private static async Task<IResult> ListInstruments(ListInstrumentsHandler handler, CancellationToken cancellationToken)
    {
        var instruments = await handler.Handle(cancellationToken);

        return TypedResults.Ok(instruments);
    }

    private static async Task<IResult> ListDrafts(ListDraftsHandler handler, CancellationToken cancellationToken)
    {
        var drafts = await handler.Handle(cancellationToken);

        return TypedResults.Ok(drafts);
    }

    private static async Task<IResult> CreateDraft(CreateDraftCommand body, CreateDraftHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var created = await handler.Handle(user.RequiredId, body, cancellationToken);

        return created.ToHttpResult();
    }

    private static async Task<IResult> ImportRules(ImportRulesCommand body, ImportRulesHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var imported = await handler.Handle(user.RequiredId, body, cancellationToken);

        return imported.ToHttpResult();
    }

    private static async Task<IResult> GetDraft(Guid draftId, GetDraftHandler handler, CancellationToken cancellationToken)
    {
        var draft = await handler.Handle(draftId, cancellationToken);

        return draft.ToHttpResult();
    }

    private static async Task<IResult> ChangeDraft(Guid draftId, DraftChangeDto body, ChangeDraftHandler handler, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(draftId, body, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<IResult> MoveNodes(Guid draftId, LayoutDto body, MoveNodesHandler handler, CancellationToken cancellationToken)
    {
        var moved = await handler.Handle(draftId, body, cancellationToken);

        return moved.ToHttpResult();
    }

    private static async Task<IResult> CheckDraft(Guid draftId, CheckDraftHandler handler, CancellationToken cancellationToken)
    {
        var check = await handler.Handle(draftId, cancellationToken);

        return check.ToHttpResult();
    }

    private static async Task<IResult> CopyEntryToExit(
        Guid draftId,
        CopyEntryToExitCommand body,
        CopyEntryToExitHandler handler,
        CancellationToken cancellationToken)
    {
        var copied = await handler.Handle(draftId, body.Revision, body.Margin, cancellationToken);

        return copied.ToHttpResult();
    }

    private static async Task<IResult> GetDraftRules(Guid draftId, GetDraftRulesHandler handler, CancellationToken cancellationToken)
    {
        var rules = await handler.Handle(draftId, cancellationToken);

        return rules.ToHttpResult();
    }

    private static async Task<IResult> ReplaceDraftRules(
        Guid draftId,
        ReplaceDraftRulesCommand body,
        ReplaceDraftRulesHandler handler,
        CancellationToken cancellationToken)
    {
        var replaced = await handler.Handle(draftId, body, cancellationToken);

        return replaced.ToHttpResult();
    }

    private static async Task<IResult> DiscardDraft(Guid draftId, DiscardDraftHandler handler, CancellationToken cancellationToken)
    {
        var discarded = await handler.Handle(draftId, cancellationToken);

        return discarded.ToHttpResult();
    }
}
