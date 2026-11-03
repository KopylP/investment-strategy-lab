namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class PublishedSource
{
    public static Result<(StrategyGraph Graph, DraftLayout Layout)> Choose(PublishedLayout? saved, AuthoredVersion version, RuleVocabulary vocabulary)
    {
        if (saved is not null && saved.Version == version.Version)
            return (saved.Graph, saved.Layout);

        var imported = GraphFromRules.Build(version.Rules, vocabulary);
        if (imported.IsFailure)
            return imported.Error;

        return (imported.Value.Graph, DraftLayout.Empty);
    }
}
