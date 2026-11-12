namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed class InvestmentAccount : AggregateRoot<AccountId>, IUserOwned
{
    private static readonly Currency[] BaseCurrencies = [Currency.EUR, Currency.USD];

    private readonly List<SubPortfolio> _subPortfolios = [];

    public UserId OwnerId { get; private set; }
    public string Name { get; private set; }
    public string Broker { get; private set; }
    public Currency BaseCurrency { get; private set; }
    public string TaxPolicyCode { get; private set; }
    public ContributionPolicy ContributionPolicy { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public IReadOnlyList<SubPortfolio> SubPortfolios => [.. _subPortfolios.OrderBy(sub => sub.Position)];

    public IEnumerable<SubPortfolio> ActiveSubPortfolios => SubPortfolios.Where(sub => sub.IsActive);

    public IReadOnlyList<(SubPortfolioId, decimal)> TargetWeights() =>
        [.. ActiveSubPortfolios.Select(sub => (sub.Id, sub.TargetShare.Value.Value))];

    private InvestmentAccount(
        AccountId id, UserId ownerId, string name, string broker, Currency baseCurrency, string taxPolicyCode,
        ContributionPolicy contributionPolicy, DateTimeOffset openedAt) : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        Broker = broker;
        BaseCurrency = baseCurrency;
        TaxPolicyCode = taxPolicyCode;
        ContributionPolicy = contributionPolicy;
        OpenedAt = openedAt;
    }

    private InvestmentAccount(AccountId id) : base(id) =>
        (Name, Broker, BaseCurrency, TaxPolicyCode, ContributionPolicy) = (null!, null!, null!, null!, null!);

    public static Result<InvestmentAccount> Open(
        UserId ownerId, string name, string broker, Currency baseCurrency, string taxPolicyCode,
        ContributionPolicy contributionPolicy, IReadOnlyList<SubPortfolioDraft> subPortfolios, DateTimeOffset openedAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            return AccountErrors.NameRequired();

        if (string.IsNullOrWhiteSpace(broker))
            return AccountErrors.BrokerRequired();

        if (!BaseCurrencies.Contains(baseCurrency))
            return AccountErrors.UnsupportedBaseCurrency(baseCurrency);

        if (subPortfolios.Count == 0)
            return AccountErrors.NoSubPortfolios();

        var account = new InvestmentAccount(
            AccountId.New(), ownerId, name.Trim(), broker.Trim(), baseCurrency,
            taxPolicyCode.Trim(), contributionPolicy, openedAt);

        foreach (var draft in subPortfolios)
        {
            if (account.CheckNameIsFree(draft.Name) is { } taken)
                return taken;

            account._subPortfolios.Add(SubPortfolio.Create(draft, account._subPortfolios.Count));
        }

        if (account.CheckSharesAddUp() is { } error)
            return error;

        account.RaiseOpened(openedAt);

        return account;
    }

    public Result<SubPortfolioId> AddSubPortfolio(
        SubPortfolioDraft draft,
        IReadOnlyDictionary<SubPortfolioId, TargetShare> sharesOfOthers,
        DateTimeOffset now)
    {
        if (CheckNameIsFree(draft.Name) is { } taken)
            return taken;

        var added = SubPortfolio.Create(draft, _subPortfolios.Count);
        var shares = new Dictionary<SubPortfolioId, TargetShare>(sharesOfOthers) { [added.Id] = draft.TargetShare };
        _subPortfolios.Add(added);

        var changed = ApplyTargetShares(shares);
        if (changed.IsFailure)
        {
            _subPortfolios.Remove(added);

            return changed.Error;
        }

        Raise(new SubPortfolioAdded(Id, added.Id, draft.Name.Trim(), now));
        RaiseSharesChanged(now);

        return added.Id;
    }

    public Result ChangeTargetShares(IReadOnlyDictionary<SubPortfolioId, TargetShare> shares, DateTimeOffset now)
    {
        var changed = ApplyTargetShares(shares);
        if (changed.IsFailure)
            return changed;

        RaiseSharesChanged(now);

        return Result.Success();
    }

    private Result ApplyTargetShares(IReadOnlyDictionary<SubPortfolioId, TargetShare> shares)
    {
        var active = ActiveSubPortfolios.ToList();
        if (shares.Count != active.Count || active.Any(sub => !shares.ContainsKey(sub.Id)))
            return AccountErrors.TargetSharesIncomplete();

        var total = shares.Values.Sum(share => share.Value.Value);
        if (total != 1m)
            return AccountErrors.TargetSharesDoNotAddUp(total * 100m);

        foreach (var sub in active)
            sub.ChangeTargetShare(shares[sub.Id]);

        return Result.Success();
    }

    public Result AssignStrategy(SubPortfolioId subPortfolioId, StrategyRef? strategy)
    {
        if (FindActive(subPortfolioId) is not { } sub)
            return AccountErrors.SubPortfolioNotFound(subPortfolioId.Value);

        sub.AssignStrategy(strategy);

        return Result.Success();
    }

    public Result RenameSubPortfolio(SubPortfolioId subPortfolioId, string name)
    {
        if (FindActive(subPortfolioId) is not { } renamed)
            return AccountErrors.SubPortfolioNotFound(subPortfolioId.Value);

        if (CheckNameIsFree(name, renamed) is { } taken)
            return taken;

        renamed.Rename(name);

        return Result.Success();
    }

    public Result RetireSubPortfolio(
        SubPortfolioId subPortfolioId,
        IReadOnlyDictionary<SubPortfolioId, TargetShare> sharesOfRemaining,
        DateTimeOffset now)
    {
        if (FindActive(subPortfolioId) is not { } sub)
            return AccountErrors.SubPortfolioNotFound(subPortfolioId.Value);

        if (ActiveSubPortfolios.Count() == 1)
            return AccountErrors.LastSubPortfolio();

        var remaining = ActiveSubPortfolios.Where(other => other.Id != subPortfolioId).ToList();
        if (sharesOfRemaining.Count != remaining.Count || remaining.Any(other => !sharesOfRemaining.ContainsKey(other.Id)))
            return AccountErrors.TargetSharesIncomplete();

        var total = sharesOfRemaining.Values.Sum(share => share.Value.Value);
        if (total != 1m)
            return AccountErrors.TargetSharesDoNotAddUp(total * 100m);

        sub.Retire();

        foreach (var other in remaining)
            other.ChangeTargetShare(sharesOfRemaining[other.Id]);

        RaiseSharesChanged(now);

        return Result.Success();
    }

    public void ChangeContributionPolicy(ContributionPolicy policy) => ContributionPolicy = policy;

    public SubPortfolio? FindActive(SubPortfolioId subPortfolioId) =>
        _subPortfolios.FirstOrDefault(sub => sub.Id == subPortfolioId && sub.IsActive);

    public bool Has(SubPortfolioId subPortfolioId) => _subPortfolios.Any(sub => sub.Id == subPortfolioId);

    private void RaiseOpened(DateTimeOffset openedAt)
    {
        foreach (var sub in SubPortfolios)
            Raise(new SubPortfolioAdded(Id, sub.Id, sub.Name, openedAt));

        RaiseSharesChanged(openedAt);
    }

    private void RaiseSharesChanged(DateTimeOffset now)
    {
        var shares = ActiveSubPortfolios.ToDictionary(sub => sub.Id.Value, sub => sub.TargetShare.Value.Value);

        Raise(new TargetSharesChanged(Id, shares, now));
    }

    private Error? CheckNameIsFree(string name, SubPortfolio? renamed = null) =>
        string.IsNullOrWhiteSpace(name) ? AccountErrors.NameRequired()
        : ActiveSubPortfolios.Any(sub => sub != renamed && string.Equals(
            sub.Name,
            name.Trim(),
            StringComparison.OrdinalIgnoreCase)) ? AccountErrors.SubPortfolioNameTaken(name.Trim())
        : null;

    private Error? CheckSharesAddUp()
    {
        var total = ActiveSubPortfolios.Sum(sub => sub.TargetShare.Value.Value);

        return total == 1m ? null : AccountErrors.TargetSharesDoNotAddUp(total * 100m);
    }
}
