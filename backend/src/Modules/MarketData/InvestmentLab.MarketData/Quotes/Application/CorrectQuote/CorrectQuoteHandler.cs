using InvestmentLab.SharedKernel;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application.Shared;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.CorrectQuote;

public sealed record CorrectQuoteCommand(Guid InstrumentId, DateOnly TradingDate, decimal Close, decimal? AdjustedClose, string Reason);

public sealed class CorrectQuoteHandler(IQuoteStore store, TimeProvider clock) : IHandler
{
    public async Task<Result> Handle(CorrectQuoteCommand command, string correctedBy, CancellationToken cancellationToken)
    {
        if (command.InstrumentId == Guid.Empty || !await store.InstrumentExistsAsync(InstrumentId.Of(command.InstrumentId), cancellationToken))
            return QuoteErrors.InstrumentNotFound(command.InstrumentId);

        var instrumentId = InstrumentId.Of(command.InstrumentId);
        var adjustedClose = command.AdjustedClose ?? command.Close;
        var now = clock.GetUtcNow();

        var correction = QuoteCorrection.Record(instrumentId, command.TradingDate, command.Close, adjustedClose, command.Reason, correctedBy, now);
        if (correction.IsFailure)
            return correction.Error;

        var existing = (await store.QuotesFromAsync(instrumentId, command.TradingDate, cancellationToken))
            .FirstOrDefault(quote => quote.TradingDate == command.TradingDate);
        if (existing is null)
        {
            existing = DailyQuote.Fetched(instrumentId, command.TradingDate, command.Close, adjustedClose, "manual", now);
            store.Add(existing);
        }

        existing.ApplyCorrection(command.Close, adjustedClose);
        store.Add(correction.Value);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
