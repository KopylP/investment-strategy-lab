namespace InvestmentLab.Performance.Valuation.Domain;

public interface IAccountJournals
{
    Task<AccountJournal?> FindAsync(Guid accountId, CancellationToken cancellationToken);
}
