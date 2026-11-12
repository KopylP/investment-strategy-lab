using InvestmentLab.SharedKernel;
using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.FailExperiment;

public sealed class FailExperimentHandler(IExperimentRepository experiments, TimeProvider clock) : IHandler
{
    public async Task Handle(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(id, cancellationToken);
        if (experiment is null)
            return;

        var now = clock.GetUtcNow();
        experiment.Fail(Message.Of("research.failed"), now);

        await experiments.SaveAsync(cancellationToken);
    }
}
