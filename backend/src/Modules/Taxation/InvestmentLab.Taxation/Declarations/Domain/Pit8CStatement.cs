namespace InvestmentLab.Taxation.Declarations.Domain;

/// <summary>
/// What a Polish broker's PIT-8C says of an account's year (poz. 35 revenue, poz. 36 costs), entered by the person:
/// PIT-38 row 1 takes these, not the journal's figures (brochure: "w poz. 20 należy wykazać kwotę z poz. 35 informacji PIT-8C").
/// </summary>
public sealed class Pit8CStatement
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public int Year { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal Costs { get; private set; }

    private Pit8CStatement(Guid accountId, UserId ownerId, int year) => (Id, AccountId, OwnerId, Year) = (Guid.CreateVersion7(), accountId, ownerId, year);

    // EF Core.
    private Pit8CStatement() { }

    public static Result<Pit8CStatement> Record(Guid accountId, UserId ownerId, int year, decimal revenue, decimal costs)
    {
        var statement = new Pit8CStatement(accountId, ownerId, year);
        var changed = statement.Change(revenue, costs);
        return changed.IsFailure ? changed.Error : statement;
    }

    public Result Change(decimal revenue, decimal costs)
    {
        if (revenue < 0m || costs < 0m)
            return Error.Validation("tax.pit8c_negative", "PIT-8C amounts are not negative.");
        (Revenue, Costs) = (decimal.Round(revenue, 2, MidpointRounding.AwayFromZero), decimal.Round(costs, 2, MidpointRounding.AwayFromZero));
        return Result.Success();
    }

    public Pit8CAmounts Amounts => new(Revenue, Costs);
}
