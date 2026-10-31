namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class TaxAssessment : AggregateRoot<Guid>
{
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

    private TaxAssessment(UserId ownerId, int year) : base(Guid.CreateVersion7())
    {
        OwnerId = ownerId;
        Year = year;
        PolicyCode = "";
        InputHash = "";
        Summary = null!;
        Realizations = [];
        Dividends = [];
        CarryforwardUses = [];
    }

    private TaxAssessment(Guid id) : base(id) => (PolicyCode, InputHash, Summary, Realizations, Dividends, CarryforwardUses) = (null!, null!, null!, null!, null!, null!);

    public static TaxAssessment Start(UserId ownerId, int year) => new(ownerId, year);

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

        Raise(new TaxAssessmentRecomputed(OwnerId, Year, result.Summary.TaxDue, now));

        return true;
    }
}
