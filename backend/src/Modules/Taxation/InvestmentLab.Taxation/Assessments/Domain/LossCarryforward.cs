namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class LossCarryforward
{
    public Guid Id { get; private set; }
    public UserId OwnerId { get; private set; }
    public int Year { get; private set; }
    public decimal Amount { get; private set; }
    public decimal AlreadyUsed { get; private set; }

    private LossCarryforward(UserId ownerId, int year, decimal amount, decimal alreadyUsed) =>
        (Id, OwnerId, Year, Amount, AlreadyUsed) = (Guid.CreateVersion7(), ownerId, year, amount, alreadyUsed);

    private LossCarryforward() { }

    public static Result<LossCarryforward> Record(UserId ownerId, int year, decimal amount, decimal alreadyUsed)
    {
        if (amount <= 0m)
            return Error.Validation("tax.loss_not_positive", "A loss is a positive amount.");

        if (alreadyUsed < 0m || alreadyUsed > amount)
            return Error.Validation("tax.loss_used_out_of_range", "Already used must be between 0 and the loss.");

        if (year is < 2000 or > 2100)
            return Error.Validation("tax.loss_year_invalid", $"{year} is not a tax year.");

        return new LossCarryforward(ownerId, year, decimal.Round(amount, 2), decimal.Round(alreadyUsed, 2));
    }

    public LossYear ToLossYear() => new(Year, Amount, AlreadyUsed);
}
