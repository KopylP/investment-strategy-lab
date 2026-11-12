using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.CheckDraft;

public sealed record DraftCheckDto(bool IsClean, IReadOnlyList<DraftProblemDto> Problems, IReadOnlyList<Guid> SignalIds);

public sealed class CheckDraftHandler(IDraftRepository drafts, DraftChecker checker) : IHandler
{
    public async Task<Result<DraftCheckDto>> Handle(Guid draftId, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        var check = await checker.CheckAsync(draft, cancellationToken);
        if (check.IsFailure)
            return check.Error;

        var problems = check.Value.Problems.Select(DraftMapping.ToDto).ToList();

        return new DraftCheckDto(check.Value.IsClean, problems, check.Value.SignalIds);
    }
}
