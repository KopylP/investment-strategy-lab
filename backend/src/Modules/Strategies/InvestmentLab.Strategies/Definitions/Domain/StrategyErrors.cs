namespace InvestmentLab.Strategies.Definitions.Domain;

public static class StrategyErrors
{
    public static Error NotFound(Guid strategyId) => Error.NotFound("strategy.not_found", $"Strategy {strategyId} does not exist.");

    public static Error VersionNotFound(Guid strategyId, int version) =>
        Error.NotFound("strategy.version_not_found", $"Strategy {strategyId} has no version {version}.");

    public static Error NameRequired() => Error.Validation("strategy.name_required", "Name must not be blank.");

    public static Error NameTaken(string name) => Error.Conflict("strategy.name_taken", $"You already have a strategy called '{name}'.");

    public static Error UniverseEmpty() => Error.Validation("strategy.universe_empty", "The universe needs at least one instrument.");

    public static Error UniverseRepeated() => Error.Validation("strategy.universe_repeated", "Each instrument may appear in the universe once.");

    public static Error InstrumentRequired() => Error.Validation("strategy.instrument_required", "Instrument ids must not be empty.");

    public static Error UnknownInstruments(IEnumerable<string> keys) =>
        Error.Validation("strategy.instrument_unknown", $"Unknown instruments: {string.Join(", ", keys)}.");

    public static Error UnknownType(string code) => Error.Validation("strategy.type_unknown", $"No engine runs the strategy type '{code}'.");

    public static Error InvalidParameters(string details) => Error.Validation("strategy.parameters_invalid", $"Parameters do not fit the strategy type: {details}");

    public static Error Archived(Guid strategyId) => Error.Conflict("strategy.archived", $"Strategy {strategyId} is archived.");
}
