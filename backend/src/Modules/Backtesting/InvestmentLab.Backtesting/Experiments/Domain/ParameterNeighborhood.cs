namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record ParameterNeighborhood(string Path, IReadOnlyList<decimal> Values)
{
    private const int MaximumValues = 5;
    private const int MaximumPathLength = 300;

    public Error? Problem()
    {
        if (string.IsNullOrWhiteSpace(Path) || Path.Length > MaximumPathLength)
            return ExperimentErrors.InvalidParameter();

        return Values.Count is < 1 or > MaximumValues ? ExperimentErrors.InvalidParameter() : null;
    }
}
