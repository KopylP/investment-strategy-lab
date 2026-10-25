namespace InvestmentLab.Taxation.Assessments.Domain;

/// <summary>
/// The tax of one account for one year (key: account + year). Rebuilt from the whole history whenever its input
/// changes; <see cref="InputHash"/> makes a recomputation with the same input a no-op.
/// </summary>
public sealed class TaxAssessment
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public int Year { get; private set; }
    public string PolicyCode { get; private set; }
    public int PolicyVersion { get; private set; }
    public string InputHash { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }
    public TaxYearSummary Summary { get; private set; }
    public List<Realization> Realizations { get; private set; }
    public List<DividendIncome> Dividends { get; private set; }
    public List<CarryforwardUse> CarryforwardUses { get; private set; }

    private TaxAssessment(Guid accountId, UserId ownerId, int year)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        OwnerId = ownerId;
        Year = year;
        PolicyCode = "";
        InputHash = "";
        Summary = null!;
        Realizations = [];
        Dividends = [];
        CarryforwardUses = [];
    }

    // EF Core.
    private TaxAssessment() => (PolicyCode, InputHash, Summary, Realizations, Dividends, CarryforwardUses) = (null!, null!, null!, null!, null!, null!);

    public static TaxAssessment Start(Guid accountId, UserId ownerId, int year) => new(accountId, ownerId, year);

    /// <returns>False when nothing changed.</returns>
    public bool Recompute(TaxYearResult result, ITaxPolicy policy, string inputHash, DateTimeOffset now)
    {
        if (InputHash == inputHash && PolicyCode == policy.Code && PolicyVersion == policy.Version)
            return false;

        PolicyCode = policy.Code;
        PolicyVersion = policy.Version;
        InputHash = inputHash;
        ComputedAt = now;
        Summary = result.Summary;
        Realizations = [.. result.Realizations];
        Dividends = [.. result.Dividends];
        CarryforwardUses = [.. result.CarryforwardUses];
        return true;
    }
}

/// <summary>A loss from a year before the journal, entered by the user (e.g. from an earlier PIT-38).</summary>
public sealed class LossCarryforward
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public int Year { get; private set; }
    public decimal Amount { get; private set; }
    public decimal AlreadyUsed { get; private set; }

    private LossCarryforward(Guid accountId, UserId ownerId, int year, decimal amount, decimal alreadyUsed) =>
        (Id, AccountId, OwnerId, Year, Amount, AlreadyUsed) = (Guid.CreateVersion7(), accountId, ownerId, year, amount, alreadyUsed);

    // EF Core.
    private LossCarryforward() { }

    public static Result<LossCarryforward> Record(Guid accountId, UserId ownerId, int year, decimal amount, decimal alreadyUsed)
    {
        if (amount <= 0m)
            return Error.Validation("tax.loss_not_positive", "A loss is a positive amount.");
        if (alreadyUsed < 0m || alreadyUsed > amount)
            return Error.Validation("tax.loss_used_out_of_range", "Already used must be between 0 and the loss.");
        if (year is < 2000 or > 2100)
            return Error.Validation("tax.loss_year_invalid", $"{year} is not a tax year.");
        return new LossCarryforward(accountId, ownerId, year, decimal.Round(amount, 2), decimal.Round(alreadyUsed, 2));
    }

    public LossYear ToLossYear() => new(Year, Amount, AlreadyUsed);
}
