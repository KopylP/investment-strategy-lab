namespace InvestmentLab.Portfolio.Accounts.Domain;

public readonly record struct SubPortfolioId
{
    public Guid Value { get; }

    private SubPortfolioId(Guid value) => Value = value;

    public static SubPortfolioId New() => new(Guid.CreateVersion7());

    public static SubPortfolioId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty sub-portfolio id.", nameof(value));

    public override string ToString() => Value.ToString();
}
