namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class ImportErrors
{
    public static Error NotJson() => Error.Validation("builder.import.not_json", "The rules are not JSON.");

    public static Error FormatVersion(string version) =>
        Error.Validation("builder.import.format_version", $"Unknown rule format version {version}.", new { version });

    public static Error FieldUnknown(string path) => Error.Validation("builder.import.field_unknown", $"Unknown field at {path}.", new { path });

    public static Error IndicatorUnknown(string kind, string path) =>
        Error.Validation("builder.import.indicator_unknown", $"Unknown indicator kind '{kind}' at {path}.", new { kind, path });

    public static Error ConditionUnknown(string kind, string path) =>
        Error.Validation("builder.import.condition_unknown", $"Unknown condition kind '{kind}' at {path}.", new { kind, path });

    public static Error ReferenceUnknown(string name, string path) =>
        Error.Validation("builder.import.reference_unknown", $"No indicator '{name}' for {path}.", new { name, path });
}
