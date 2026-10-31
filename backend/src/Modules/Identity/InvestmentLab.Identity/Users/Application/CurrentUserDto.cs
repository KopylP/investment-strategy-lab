namespace InvestmentLab.Identity.Users.Application;

public sealed record CurrentUserDto(Guid Id, string Email, bool ViaHomeAssistant = false);
