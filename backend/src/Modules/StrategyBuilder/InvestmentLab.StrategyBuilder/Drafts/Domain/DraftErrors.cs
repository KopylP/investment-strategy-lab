namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class DraftErrors
{
    public static Error NotFound(Guid draftId) => Error.NotFound("builder.draft.not_found", $"Draft {draftId} does not exist.");

    public static Error Stale() => Error.Conflict("builder.draft.stale", "The draft was changed since this revision.");

    public static Error NameRequired() => Error.Validation("builder.draft.name_required", "A draft needs a name.");

    public static Error TooManyFunds(int max) => Error.Validation("builder.setup.too_many_funds", $"A draft takes at most {max} funds.", new { max });

    public static Error FundRepeated() => Error.Validation("builder.setup.fund_repeated", "Each fund may appear once.");

    public static Error TemplateUnknown(string template) =>
        Error.Validation("builder.template.unknown", $"No template '{template}'.", new { template });

    public static Error GraphInvalid(string details) =>
        Error.Validation("builder.draft.graph_invalid", $"The graph cannot be read: {details}", new { details });

    public static Error HasProblems() => Error.Conflict("builder.draft.has_problems", "The draft has problems to fix first.");

    public static Error VocabularyUnavailable() =>
        Error.Conflict("builder.vocabulary.unavailable", "The rule-based strategy type is not in the catalog.");
}
