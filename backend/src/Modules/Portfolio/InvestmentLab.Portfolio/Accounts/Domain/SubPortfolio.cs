namespace InvestmentLab.Portfolio.Accounts.Domain;

/// <summary>A virtual part of the account with its own strategy, target share and holdings.</summary>
public sealed class SubPortfolio : Entity<SubPortfolioId>
{
    public string Name { get; private set; }
    public TargetShare TargetShare { get; private set; }
    public StrategyRef? Strategy { get; private set; }

    /// <summary>ETF where the strategy parks money it does not invest (e.g. XEON).</summary>
    public Guid? CashInstrumentId { get; private set; }

    public Guardrails Guardrails { get; private set; }
    public SubPortfolioStatus Status { get; private set; }

    /// <summary>Place in the account, so sub-portfolios are always listed in the order they were added.</summary>
    public int Position { get; private set; }

    private SubPortfolio(SubPortfolioId id, int position, string name, TargetShare targetShare, StrategyRef? strategy, Guid? cashInstrumentId, Guardrails guardrails)
        : base(id)
    {
        Position = position;
        Name = name;
        TargetShare = targetShare;
        Strategy = strategy;
        CashInstrumentId = cashInstrumentId;
        Guardrails = guardrails;
        Status = SubPortfolioStatus.Active;
    }

    // EF Core: owned types are materialised through this constructor.
    private SubPortfolio(SubPortfolioId id) : base(id) => (Name, TargetShare, Guardrails) = (null!, null!, null!);

    internal static SubPortfolio Create(SubPortfolioDraft draft, int position) =>
        new(SubPortfolioId.New(), position, draft.Name.Trim(), draft.TargetShare, draft.Strategy, draft.CashInstrumentId, draft.Guardrails);

    public bool IsActive => Status == SubPortfolioStatus.Active;

    internal void ChangeTargetShare(TargetShare share) => TargetShare = share;

    internal void AssignStrategy(StrategyRef? strategy) => Strategy = strategy;

    internal void Retire() => Status = SubPortfolioStatus.Retired;
}
