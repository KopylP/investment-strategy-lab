using InvestmentLab.MarketData.Contracts.Quotes;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed record MarketSeries(Guid InstrumentId, SeriesRole Role, IReadOnlyList<QuotePointDto> Points);
