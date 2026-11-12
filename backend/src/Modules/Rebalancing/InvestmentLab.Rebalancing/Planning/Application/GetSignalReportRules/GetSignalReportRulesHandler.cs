using InvestmentLab.SharedKernel;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GetSignalReportRules;

public sealed class GetSignalReportRulesHandler(ISignalReportRuleRepository rules) : IHandler
{
    public async Task<Result<IReadOnlyList<SignalReportRuleDto>>> Handle(CancellationToken cancellationToken)
    {
        var all = await rules.AllAsync(cancellationToken);
        var dtos = all.Select(SignalReportRuleDto.Of).ToList();

        return Result<IReadOnlyList<SignalReportRuleDto>>.Success(dtos);
    }
}
