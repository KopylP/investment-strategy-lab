namespace InvestmentLab.Identity.Users.Application.Shared;

public sealed record CurrentUserDto(Guid Id, string Email, bool ViaHomeAssistant = false);
