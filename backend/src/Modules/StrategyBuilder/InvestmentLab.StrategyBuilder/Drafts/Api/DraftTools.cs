using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.StrategyBuilder.Drafts.Application.ChangeDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.CheckDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.CopyEntryToExit;
using InvestmentLab.StrategyBuilder.Drafts.Application.CreateDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.DiscardDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetDraftRules;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetPublishedStrategy;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetVocabulary;
using InvestmentLab.StrategyBuilder.Drafts.Application.ImportRules;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListDrafts;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListOwnSignals;
using InvestmentLab.StrategyBuilder.Drafts.Application.OpenPublishedStrategy;
using InvestmentLab.StrategyBuilder.Drafts.Application.PublishDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.ReplaceDraftRules;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using ModelContextProtocol.Server;

namespace InvestmentLab.StrategyBuilder.Drafts.Api;

[McpServerToolType]
internal static class DraftTools
{
    [McpServerTool(Name = "get_vocabulary", ReadOnly = true)]
    [Description("The building blocks of strategy rules: indicators, conditions, selection, weights and risk blocks with their parameters.")]
    public static async Task<VocabularyDto> GetVocabulary(GetVocabularyHandler handler, CancellationToken cancellationToken)
    {
        var vocabulary = await handler.Handle(cancellationToken);

        return vocabulary.ValueOrThrow();
    }

    [McpServerTool(Name = "list_instruments", ReadOnly = true)]
    [Description("Instruments a strategy can trade or watch, with their ids.")]
    public static Task<IReadOnlyList<InstrumentChoiceDto>> ListInstruments(ListInstrumentsHandler handler, CancellationToken cancellationToken) =>
        handler.Handle(cancellationToken);

    [McpServerTool(Name = "list_own_signals", ReadOnly = true)]
    [Description("The user's own signals that strategy rules can use.")]
    public static Task<IReadOnlyList<OwnSignalDto>> ListOwnSignals(ListOwnSignalsHandler handler, CancellationToken cancellationToken) =>
        handler.Handle(cancellationToken);

    [McpServerTool(Name = "list_drafts", ReadOnly = true)]
    [Description("The user's strategy drafts.")]
    public static Task<IReadOnlyList<DraftSummaryDto>> ListDrafts(ListDraftsHandler handler, CancellationToken cancellationToken) =>
        handler.Handle(cancellationToken);

    [McpServerTool(Name = "get_draft", ReadOnly = true)]
    [Description("One strategy draft: setup, graph, reports, trials and its current revision.")]
    public static async Task<DraftDto> GetDraft(Guid draftId, GetDraftHandler handler, CancellationToken cancellationToken)
    {
        var draft = await handler.Handle(draftId, cancellationToken);

        return draft.ValueOrThrow();
    }

    [McpServerTool(Name = "get_draft_rules", ReadOnly = true)]
    [Description("A draft's rules as JSON (formatVersion 1), the same format import_rules and replace_draft_rules take. Fails while the draft has errors.")]
    public static async Task<DraftRulesDto> GetDraftRules(Guid draftId, GetDraftRulesHandler handler, CancellationToken cancellationToken)
    {
        var rules = await handler.Handle(draftId, cancellationToken);

        return rules.ValueOrThrow();
    }

    [McpServerTool(Name = "create_draft")]
    [Description("Starts a new strategy draft, empty or from a template.")]
    public static async Task<DraftDto> CreateDraft(
        string name,
        [Description("Template code, or null for an empty draft.")] string? template,
        CreateDraftHandler handler,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        var command = new CreateDraftCommand(name, template);

        var created = await handler.Handle(user.RequiredId, command, cancellationToken);

        return created.ValueOrThrow();
    }

    [McpServerTool(Name = "import_rules")]
    [Description("Creates a new draft from rules JSON (formatVersion 1).")]
    public static async Task<DraftDto> ImportRules(
        string name,
        [Description("The rules as a JSON document.")] string rulesJson,
        ImportRulesHandler handler,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        var command = new ImportRulesCommand(name, rulesJson);

        var imported = await handler.Handle(user.RequiredId, command, cancellationToken);

        return imported.ValueOrThrow();
    }

    [McpServerTool(Name = "replace_draft_rules")]
    [Description("Replaces an existing draft's rules from rules JSON, keeping its funds, reports and trials.")]
    public static async Task<DraftDto> ReplaceDraftRules(
        Guid draftId,
        [Description("The draft's current revision, from get_draft.")] int revision,
        [Description("The rules as a JSON document.")] string rulesJson,
        ReplaceDraftRulesHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ReplaceDraftRulesCommand(revision, rulesJson);

        var replaced = await handler.Handle(draftId, command, cancellationToken);

        return replaced.ValueOrThrow();
    }

    [McpServerTool(Name = "change_draft")]
    [Description("Changes a draft's name, description or setup (funds, benchmark, cash, volatility index, extra series). Fields left null stay as they are.")]
    public static async Task<DraftDto> ChangeDraft(
        Guid draftId,
        [Description("The draft's current revision, from get_draft.")] int revision,
        string? name,
        string? description,
        DraftSetupDto? setup,
        ChangeDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var change = new DraftChangeDto(
            Revision: revision,
            Name: name,
            Description: description,
            Setup: setup,
            Graph: null,
            Reports: null);

        var changed = await handler.Handle(draftId, change, cancellationToken);

        return changed.ValueOrThrow();
    }

    [McpServerTool(Name = "copy_entry_to_exit")]
    [Description("Builds the exit rule as the mirror of the entry rule with a margin.")]
    public static async Task<DraftDto> CopyEntryToExit(
        Guid draftId,
        [Description("The draft's current revision, from get_draft.")] int revision,
        [Description("Margin between entry and exit thresholds, as a fraction.")] double margin,
        CopyEntryToExitHandler handler,
        CancellationToken cancellationToken)
    {
        var copied = await handler.Handle(draftId, revision, margin, cancellationToken);

        return copied.ValueOrThrow();
    }

    [McpServerTool(Name = "check_draft", ReadOnly = true)]
    [Description("Checks a draft and lists its problems. A trial or publishing needs a draft without errors.")]
    public static async Task<DraftCheckDto> CheckDraft(Guid draftId, CheckDraftHandler handler, CancellationToken cancellationToken)
    {
        var check = await handler.Handle(draftId, cancellationToken);

        return check.ValueOrThrow();
    }

    [McpServerTool(Name = "publish_draft", Destructive = true)]
    [Description("Publishes a draft as a new strategy version the user's sub-portfolios can follow. Only when the user asks.")]
    public static async Task<PublishedVersionDto> PublishDraft(
        Guid draftId,
        [Description("The draft's current revision, from get_draft.")] int revision,
        PublishDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var published = await handler.Handle(draftId, revision, cancellationToken);

        return published.ValueOrThrow();
    }

    [McpServerTool(Name = "discard_draft", Destructive = true)]
    [Description("Deletes a draft with its trials.")]
    public static async Task<string> DiscardDraft(Guid draftId, DiscardDraftHandler handler, CancellationToken cancellationToken)
    {
        var discarded = await handler.Handle(draftId, cancellationToken);
        discarded.ThrowIfFailed();

        return ToolReplies.Done;
    }

    [McpServerTool(Name = "get_published_strategy", ReadOnly = true)]
    [Description("A published version of a strategy made in the builder.")]
    public static async Task<PublishedStrategyDto> GetPublishedStrategy(
        Guid strategyId,
        int version,
        GetPublishedStrategyHandler handler,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        var published = await handler.Handle(user.RequiredId, strategyId, version, cancellationToken);

        return published.ValueOrThrow();
    }

    [McpServerTool(Name = "open_published_strategy")]
    [Description("Opens a published strategy version as a new draft to change it.")]
    public static async Task<DraftDto> OpenPublishedStrategy(
        Guid strategyId,
        int version,
        OpenPublishedStrategyHandler handler,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        var draft = await handler.Handle(user.RequiredId, strategyId, version, cancellationToken);

        return draft.ValueOrThrow();
    }
}
