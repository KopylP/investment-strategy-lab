using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Experiments.Application.Shared;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentJournal;

public sealed record GetExperimentJournalQuery(Guid ExperimentId, string? Kind, DateOnly? From, DateOnly? To, int Offset, int Limit, bool Detail = true);

public sealed record ExperimentJournalDto(int Total, int Offset, IReadOnlyList<ResearchJournalEntryDto> Entries);

public sealed class GetExperimentJournalHandler(IExperimentRepository experiments) : IHandler
{
    public async Task<Result<ExperimentJournalDto>> Handle(GetExperimentJournalQuery query, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(query.ExperimentId, cancellationToken);
        if (experiment is null)
            return ExperimentErrors.NotFound(query.ExperimentId);

        var stored = experiment.Result?.Baseline.Journal ?? [];
        var journal = new ReplayJournal(stored);

        var filter = new JournalFilter(query.Kind, query.From, query.To);
        var pagination = new JournalPage(query.Offset, query.Limit);
        var page = journal.Page(filter, pagination);

        var entries = query.Detail ? page.Entries : [.. page.Entries.Select(entry => entry.WithoutCandidates())];

        return new ExperimentJournalDto(page.Total, page.Offset, [.. entries.Select(ResearchJournalEntryDto.From)]);
    }
}
