using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GetSignalReportRules;

public sealed class GetSignalReportRulesHandler(ISignalReportRuleRepository rules)
{
    public async Task<Result<IReadOnlyList<SignalReportRuleDto>>> Handle(CancellationToken cancellationToken)
    {
        var all = await rules.AllAsync(cancellationToken);
        var dtos = all.Select(SignalReportRuleDto.Of).ToList();

        return Result<IReadOnlyList<SignalReportRuleDto>>.Success(dtos);
    }
}
