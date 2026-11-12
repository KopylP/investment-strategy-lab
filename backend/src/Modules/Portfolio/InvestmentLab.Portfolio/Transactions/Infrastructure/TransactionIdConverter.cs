using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class TransactionIdConverter() : ValueConverter<TransactionId, Guid>(id => id.Value, value => TransactionId.Of(value));
