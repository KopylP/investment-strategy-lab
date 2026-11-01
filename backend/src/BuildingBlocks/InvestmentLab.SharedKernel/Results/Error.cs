namespace InvestmentLab.SharedKernel;

public sealed record Error(string Code, string Message, ErrorKind Kind, object? Parameters = null)
{
    public static Error Validation(string code, string message, object? parameters = null) => new(code, message, ErrorKind.Validation, parameters);
    public static Error NotFound(string code, string message, object? parameters = null) => new(code, message, ErrorKind.NotFound, parameters);
    public static Error Conflict(string code, string message, object? parameters = null) => new(code, message, ErrorKind.Conflict, parameters);
    public static Error Forbidden(string code, string message, object? parameters = null) => new(code, message, ErrorKind.Forbidden, parameters);

    public string AsMessage() => SharedKernel.Message.Of(Code, Parameters);
}
