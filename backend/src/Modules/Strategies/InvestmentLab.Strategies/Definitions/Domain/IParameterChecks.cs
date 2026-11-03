namespace InvestmentLab.Strategies.Definitions.Domain;

public interface IParameterChecks
{
    Task<Result<ParametersCheck>> CheckAsync(string typeCode, string parameters, CancellationToken cancellationToken);
}
