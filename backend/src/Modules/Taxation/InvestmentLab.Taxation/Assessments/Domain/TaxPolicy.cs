namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ILotMatcher
{
    string Name { get; }

    IReadOnlyList<LotMatch> Take(List<OpenLot> lots, decimal quantity);
}
