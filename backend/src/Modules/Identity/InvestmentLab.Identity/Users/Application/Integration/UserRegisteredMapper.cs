using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Identity.Contracts;
using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Users.Application.Integration;

internal sealed class UserRegisteredMapper : IDomainEventMapper<UserRegistered, UserRegisteredV1>
{
    public UserRegisteredV1 Map(UserRegistered registered) =>
        new(registered.UserId.Value, registered.Email, registered.OccurredAt);
}
