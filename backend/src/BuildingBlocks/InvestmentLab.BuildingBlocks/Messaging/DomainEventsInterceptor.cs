using InvestmentLab.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InvestmentLab.BuildingBlocks.Messaging;

public sealed class DomainEventsInterceptor(DomainEventsToOutbox outbox) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        MoveToOutbox(eventData);

        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        MoveToOutbox(eventData);

        return ValueTask.FromResult(result);
    }

    private void MoveToOutbox(DbContextEventData eventData)
    {
        if (eventData.Context is ModuleDbContext context)
            outbox.Move(context);
    }
}
