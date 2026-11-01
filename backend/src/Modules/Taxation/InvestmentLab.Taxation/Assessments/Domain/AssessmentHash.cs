using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InvestmentLab.Taxation.Assessments.Domain;

public static class AssessmentHash
{
    public static string For(int year, IReadOnlyList<TaxEvent> events, IReadOnlyList<LossYear> losses, ITaxPolicy policy)
    {
        var yearEvents = events.Where(each => each.OccurredOn.Year <= year).Select(each => new
        {
            each.TransactionId,
            each.AccountId,
            each.OccurredOn,
            each.Kind,
            each.Quantity,
            each.Price,
            each.Fee,
            each.Amount,
            each.FxRate,
            each.Allocations,
            each.Broker,
            each.Withholding,
            each.Currency,
        });

        var input = JsonSerializer.Serialize(new
        {
            policy = $"{policy.Code}/{policy.Version}",
            events = yearEvents,
            losses,
        });

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
