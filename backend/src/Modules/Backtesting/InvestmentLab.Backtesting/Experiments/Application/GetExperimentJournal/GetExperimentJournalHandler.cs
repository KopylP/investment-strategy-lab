using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentJournal;

public sealed record GetExperimentJournalQuery(Guid ExperimentId, string? Kind, DateOnly? From, DateOnly? To, int Offset, int Limit);

public sealed record ExperimentJournalDto(int Total, int Offset, IReadOnlyList<ReplayJournalEntry> Entries);

public sealed class GetExperimentJournalHandler(IExperimentRepository experiments)
{
    public async Task<Result<ExperimentJournalDto>> Handle(GetExperimentJournalQuery query, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(query.ExperimentId, cancellationToken);
        if (experiment is null)
            return ExperimentErrors.NotFound(query.ExperimentId);

        var entries = experiment.Result?.Baseline.Journal ?? [];
        var journal = new ReplayJournal(entries);

        var filter = new JournalFilter(query.Kind, query.From, query.To);
        var pagination = new JournalPage(query.Offset, query.Limit);
        var page = journal.Page(filter, pagination);

        return new ExperimentJournalDto(page.Total, page.Offset, page.Entries);
    }
}
