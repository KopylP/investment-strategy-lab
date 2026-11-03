using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Evaluation.Domain;

public static class SignalStates
{
    public static Result<IReadOnlyList<SignalState>> For(IReadOnlyList<Guid> referenced, IReadOnlyList<SignalState> known)
    {
        var states = new List<SignalState>();

        foreach (var id in referenced.Distinct())
        {
            var state = known.FirstOrDefault(state => state.SignalId == id);
            if (state is null)
                return StrategyErrors.SignalDeleted(id);

            states.Add(state);
        }

        return states;
    }
}
