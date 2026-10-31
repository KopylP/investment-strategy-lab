namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed class Pit38BrokerRows
{
    private readonly int _year;
    private readonly List<DeclarationAccount> _withPit8C;
    private readonly List<IGrouping<string?, DeclarationAccount>> _brokers;

    public Pit38BrokerRows(int year, List<DeclarationAccount> taxed)
    {
        _year = year;
        _withPit8C = [.. taxed.Where(account => PolishBrokers.IssuesPit8C(account.Broker))];
        WithoutPit8CForm = [.. taxed.Except(_withPit8C)];
        _brokers = [.. _withPit8C.GroupBy(account => PolishBrokers.Key(account.Broker))];
    }

    public IReadOnlyList<DeclarationAccount> WithPit8CForm => _withPit8C;

    public IReadOnlyList<DeclarationAccount> WithoutPit8CForm { get; }

    public bool TakesPit8C(DeclarationAccount account) => _withPit8C.Contains(account);

    public List<string> BrokersMissingPit8C() =>
        [.. _brokers.Where(MissesPit8C).Select(broker => broker.Key!)];

    public (decimal Revenue, decimal Costs) Row1Of(int year) =>
        _brokers.Aggregate((Revenue: 0m, Costs: 0m), (total, broker) => WithBroker(total, broker, year));

    public string? Pit8CCoveredBy(DeclarationAccount account)
    {
        if (!TakesPit8C(account) || account.Pit8COf(_year) is not null)
            return null;

        var sameBroker = _withPit8C.Where(other => PolishBrokers.Key(other.Broker) == PolishBrokers.Key(account.Broker));

        return EnteredOn(sameBroker, _year)?.Name;
    }

    private bool MissesPit8C(IGrouping<string?, DeclarationAccount> broker) =>
        EnteredOn(broker, _year) is null && broker.Any(account => account.In(_year) is { Revenue: > 0m });

    private static (decimal Revenue, decimal Costs) WithBroker(
        (decimal Revenue, decimal Costs) total, IEnumerable<DeclarationAccount> broker, int year)
    {
        if (EnteredOn(broker, year)?.Pit8COf(year) is { } entered)
            return (total.Revenue + entered.Revenue, total.Costs + entered.Costs);

        var revenue = broker.Sum(account => account.In(year)?.Revenue ?? 0m);
        var costs = broker.Sum(account => account.In(year)?.Costs ?? 0m);

        return (total.Revenue + revenue, total.Costs + costs);
    }

    private static DeclarationAccount? EnteredOn(IEnumerable<DeclarationAccount> broker, int year) =>
        broker.FirstOrDefault(account => account.Pit8COf(year) is not null);
}
