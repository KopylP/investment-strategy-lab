namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public interface IBrokerStatementParser
{
    string Broker { get; }

    Result<ParsedBrokerStatement> Parse(byte[] content);
}
