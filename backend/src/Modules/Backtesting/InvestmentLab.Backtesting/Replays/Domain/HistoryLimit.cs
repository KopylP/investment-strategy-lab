namespace InvestmentLab.Backtesting.Replays.Domain;

public abstract record HistoryLimit
{
    public abstract string Describe(Func<Guid, string> labelOf);
}
