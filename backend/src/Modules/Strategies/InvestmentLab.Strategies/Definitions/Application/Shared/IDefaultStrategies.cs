namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public interface IDefaultStrategies
{
    IReadOnlyList<DefaultStrategy> Load();
}
