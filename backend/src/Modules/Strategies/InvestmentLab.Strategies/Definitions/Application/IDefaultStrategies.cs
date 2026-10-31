namespace InvestmentLab.Strategies.Definitions.Application;

public interface IDefaultStrategies
{
    IReadOnlyList<DefaultStrategy> Load();
}
