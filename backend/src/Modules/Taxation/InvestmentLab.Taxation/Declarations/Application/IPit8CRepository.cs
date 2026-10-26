using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

public interface IPit8CRepository
{
    /// <summary>The signed-in person's PIT-8C amounts of a year, all accounts.</summary>
    Task<IReadOnlyList<Pit8CStatement>> ForYearAsync(int year, CancellationToken cancellationToken);

    void Add(Pit8CStatement statement);

    void Remove(Pit8CStatement statement);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
