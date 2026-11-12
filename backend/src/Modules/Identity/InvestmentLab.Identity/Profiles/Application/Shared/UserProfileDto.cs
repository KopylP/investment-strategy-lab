namespace InvestmentLab.Identity.Profiles.Application.Shared;

public sealed record UserProfileDto(string DisplayCurrency, string? Language, string TimeZone, string ChartStyle);
