namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftProblem(
    string Code,
    object? Parameters,
    NodeId? Node,
    BlockKind? Block,
    string? ReportCode,
    ProblemSeverity Severity,
    string? MessageOverride = null)
{
    public string Message => MessageOverride ?? SharedKernel.Message.Of(Code, Parameters);

    public static DraftProblem OnNode(string code, GraphNode node, object? parameters = null, ProblemSeverity severity = ProblemSeverity.Error) =>
        new(code, parameters, node.Id, NodeKinds.IsResult(node.Kind) ? node.Block : null, null, severity);

    public static DraftProblem OnDraft(string code, object? parameters = null) =>
        new(code, parameters, null, null, null, ProblemSeverity.Error);
}
