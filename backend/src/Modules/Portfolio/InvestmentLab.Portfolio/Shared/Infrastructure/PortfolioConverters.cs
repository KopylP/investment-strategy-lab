using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Shared.Infrastructure;

internal static class PortfolioConverters
{
    public sealed class AccountIdConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<AccountId, Guid>(
        id => id.Value, value => AccountId.Of(value));

    public sealed class SubPortfolioIdConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<SubPortfolioId, Guid>(
        id => id.Value, value => SubPortfolioId.Of(value));

    public sealed class TargetShareConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TargetShare, decimal>(
        share => share.Value.Value, value => TargetShare.Of(Percentage.Of(value)));
}
