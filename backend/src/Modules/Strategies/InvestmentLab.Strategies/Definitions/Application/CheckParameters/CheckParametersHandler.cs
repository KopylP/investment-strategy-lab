using System.Text.Json;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.CheckParameters;

public sealed class CheckParametersHandler(StrategySetupFactory setups)
{
    public async Task<Result<ParametersCheck>> Handle(string typeCode, string parameters, CancellationToken cancellationToken)
    {
        var given = JsonDocument.Parse(parameters).RootElement;

        return await setups.CheckAsync(typeCode, given, cancellationToken);
    }
}
