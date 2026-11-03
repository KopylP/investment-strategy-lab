using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed class DraftDescriber(IRuleVocabulary vocabularies, IInstrumentDirectory instruments, IOwnSignals signals)
{
    public async Task<Result<DraftDto>> DescribeAsync(StrategyDraft draft, CancellationToken cancellationToken, IReadOnlyList<DraftProblem>? checkedProblems = null)
    {
        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var labels = await LabelsAsync(cancellationToken);
        var own = await signals.AllAsync(cancellationToken);
        var problems = checkedProblems ?? GraphCheck.Of(draft, vocabulary.Value, own);
        var words = StrategyInWords.Of(draft, vocabulary.Value, labels, own);

        return DraftMapping.ToDto(draft, problems, words);
    }

    private async Task<InstrumentLabels> LabelsAsync(CancellationToken cancellationToken)
    {
        var all = await instruments.AllAsync(cancellationToken);

        return new InstrumentLabels(all.ToDictionary(instrument => instrument.Id, instrument => instrument.Label));
    }
}
