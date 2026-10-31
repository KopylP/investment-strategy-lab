namespace InvestmentLab.Portfolio.Transactions.Domain;

public interface IStatementParser
{
    string Broker { get; }

    Result<ParsedStatement> Parse(string content);
}
