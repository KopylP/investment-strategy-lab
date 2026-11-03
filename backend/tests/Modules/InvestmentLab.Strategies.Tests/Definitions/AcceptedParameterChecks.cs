using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Tests.Definitions;

internal sealed class AcceptedParameterChecks : IParameterChecks
{
    public int Calls { get; private set; }

    public Task<Result<ParametersCheck>> CheckAsync(string typeCode, string parameters, CancellationToken cancellationToken)
    {
        Calls++;

        return Task.FromResult<Result<ParametersCheck>>(new ParametersCheck([], []));
    }
}
