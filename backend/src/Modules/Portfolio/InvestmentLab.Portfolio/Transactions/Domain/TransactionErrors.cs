namespace InvestmentLab.Portfolio.Transactions.Domain;

public static class TransactionErrors
{
    public static Error NotFound(Guid transactionId) => Error.NotFound("transaction.not_found", $"Transaction {transactionId} does not exist.");

    public static Error DuplicateSource(string reference) =>
        Error.Conflict("transaction.duplicate_source", $"A transaction from '{reference}' is already recorded.");

    public static Error QuantityNotPositive() => Error.Validation("transaction.quantity_not_positive", "Quantity must be above zero.");

    public static Error AmountNotPositive() => Error.Validation("transaction.amount_not_positive", "Amount must be above zero.");

    public static Error FeeNegative() => Error.Validation("transaction.fee_negative", "Fee cannot be negative.");

    public static Error CurrencyMismatch(Currency expected, Currency actual) =>
        Error.Validation("transaction.currency_mismatch", $"The account is kept in {expected}, not {actual}.");

    public static Error InstrumentRequired() => Error.Validation("transaction.instrument_required", "Instrument must not be empty.");

    public static Error AllocationsDoNotAddUp(decimal allocated, decimal total) =>
        Error.Validation("transaction.allocations_do_not_add_up", $"Allocations add up to {allocated}, not {total}.");

    public static Error AllocationRepeated() =>
        Error.Validation("transaction.allocation_repeated", "Each sub-portfolio may appear only once in the allocations.");

    public static Error UnknownSubPortfolio(Guid subPortfolioId) =>
        Error.Validation("transaction.sub_portfolio_unknown", $"Sub-portfolio {subPortfolioId} is not part of the account.");

    public static Error TransferToItself() =>
        Error.Validation("transaction.transfer_to_itself", "An internal transfer needs two different sub-portfolios.");

    public static Error TransferNeedsInstrumentOrCash() =>
        Error.Validation("transaction.transfer_body", "Transfer either a quantity of an instrument or an amount of cash.");

    public static Error NotEnoughUnits(Guid subPortfolioId, Guid instrumentId, decimal held, decimal requested, DateOnly on) =>
        Error.Validation("transaction.not_enough_units",
            $"On {on:yyyy-MM-dd} sub-portfolio {subPortfolioId} holds {held} of instrument {instrumentId}, cannot give away {requested}.");

    public static Error NothingToAllocateTo(Guid instrumentId) =>
        Error.Validation("transaction.nothing_to_sell", $"No sub-portfolio holds instrument {instrumentId}.");

    public static Error CannotReverseReversal() => Error.Validation("transaction.reverse_reversal", "A reversal cannot be reversed.");

    public static Error AlreadyReversed(Guid transactionId) =>
        Error.Conflict("transaction.already_reversed", $"Transaction {transactionId} is already reversed.");

    public static Error ReasonRequired() => Error.Validation("transaction.reason_required", "Say why the transaction is reversed.");
}
