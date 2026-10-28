namespace InvestmentLab.Taxation.Assessments.Application;

/// <summary>
/// The dashboard's warning: this year's tax above <see cref="Share"/> (0..1) of all accounts' value. One value for
/// everyone, from configuration (<c>Taxation:AlertShare</c>); missing or out of range — no warning.
/// </summary>
public sealed record TaxAlertOptions(decimal? Share);
