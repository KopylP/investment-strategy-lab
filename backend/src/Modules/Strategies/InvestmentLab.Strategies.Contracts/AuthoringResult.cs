namespace InvestmentLab.Strategies.Contracts;

public sealed record AuthoringResult<T>(T? Value, string? ErrorCode = null, string? ErrorMessage = null, IReadOnlyList<ParameterProblemDto>? Problems = null)
{
    public bool IsSuccess => ErrorCode is null;
}
