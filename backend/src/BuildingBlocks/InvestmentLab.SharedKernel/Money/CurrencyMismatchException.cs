namespace InvestmentLab.SharedKernel;

public sealed class CurrencyMismatchException(Currency expected, Currency actual)
    : InvalidOperationException($"Cannot combine {actual} with {expected}.");
