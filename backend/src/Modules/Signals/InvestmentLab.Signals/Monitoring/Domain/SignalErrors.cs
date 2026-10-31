namespace InvestmentLab.Signals.Monitoring.Domain;

public static class SignalErrors
{
    public static Error NotFound(Guid signalId) =>
        Error.NotFound("signal.not_found", $"Сигналу {signalId} немає.");

    public static Error InvalidName() =>
        Error.Validation("signal.name", $"Назва сигналу має містити від 1 до {Signal.MaxNameLength} символів.");

    public static Error UnknownTarget() =>
        Error.Validation("signal.target", "Виберіть, за чим стежить сигнал.");

    public static Error UnknownMetric() =>
        Error.Validation("signal.metric", "Виберіть метрику сигналу.");

    public static Error MetricDoesNotFitTarget() =>
        Error.Validation("signal.metric", "Така метрика не підходить для цього об'єкта.");

    public static Error FallThresholdOutOfRange() =>
        Error.Validation("signal.threshold", "Поріг падіння має бути більшим за 0% і не більшим за 100%.");

    public static Error LevelThresholdOutOfRange() =>
        Error.Validation("signal.threshold", "Поріг має бути більшим за нуль.");

    public static Error RearmNotBeforeThreshold() =>
        Error.Validation("signal.rearm", "Рівень повторного зведення має бути менш жорстким за поріг.");

    public static Error WindowTooShort() =>
        Error.Validation("signal.window", "Вікно пошуку піку має бути довшим за один день.");

    public static Error AccountNotFound() =>
        Error.NotFound("signal.account", "Такого рахунку немає.");

    public static Error SubPortfolioNotFound() =>
        Error.NotFound("signal.sub_portfolio", "Такого під-портфеля на рахунку немає.");

    public static Error InstrumentNotFound() =>
        Error.NotFound("signal.instrument", "Такого інструмента немає.");

    public static Error BuiltInCannotBeDeleted() =>
        Error.Conflict("signal.built_in", "Вбудований сигнал не можна видалити, але можна вимкнути.");
}
