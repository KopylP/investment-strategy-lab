using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record ReverseTransactionCommand(string Reason, DateOnly? ReversedOn = null);
