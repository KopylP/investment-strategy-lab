namespace InvestmentLab.Signals.Contracts;

public interface IBuiltInSignalReplay
{
    IBuiltInSignalWatch Watch(string kind);
}
