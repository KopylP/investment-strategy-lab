namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class GraphCheck
{
    public static IReadOnlyList<DraftProblem> Of(StrategyDraft draft, RuleVocabulary vocabulary, IReadOnlyList<OwnSignal>? signals = null)
    {
        var graph = draft.Graph;
        var effectiveNodes = graph.Nodes.Select(node => SetupBindings.Resolve(node, graph, draft.Setup)).ToList();

        return
        [
            .. SetupProblems(draft.Setup),
            .. LinkProblems(graph),
            .. BindingProblems(graph, draft.Setup),
            .. PortfolioProblems(graph),
            .. CycleProblems(graph),
            .. MissingInputs(graph),
            .. effectiveNodes.SelectMany(node => ParameterProblems(node, graph, vocabulary)),
            .. effectiveNodes.SelectMany(node => SeriesProblems(node, graph, SetupBindings.EvaluationSetup(draft))),
            .. graph.Nodes.SelectMany(node => SignalProblems(node, graph, draft.MissingSignalIds, signals ?? [])),
            .. ReportSignalProblems(draft, signals ?? []),
            .. DanglingNodes(graph),
        ];
    }

    private static IEnumerable<DraftProblem> SetupProblems(DraftSetup setup)
    {
        if (setup.Funds.Count == 0)
            yield return DraftProblem.OnDraft("builder.problem.funds_missing");
        if (setup.BenchmarkId is null)
            yield return DraftProblem.OnDraft("builder.problem.benchmark_missing");
        if (setup.CashId is null)
            yield return DraftProblem.OnDraft("builder.problem.cash_missing");
    }

    private static IEnumerable<DraftProblem> LinkProblems(StrategyGraph graph)
    {
        foreach (var link in graph.Links)
        {
            if (graph.Node(link.From) is not { } from || graph.Node(link.To) is not { } to)
                continue;

            if (SocketMismatch(from, to, link.Input, link.Output) is { } mismatch)
                yield return mismatch;
        }
    }

    private static DraftProblem? SocketMismatch(GraphNode from, GraphNode to, string inputName, string outputName)
    {
        var input = NodeSockets.InputsOf(to.Kind).FirstOrDefault(candidate => candidate.Name == inputName);
        var output = NodeSockets.OutputOf(from, outputName);
        // The selected funds are one source: rules consume the list, indicators its price series.
        if (input is not null && (output == input.Type ||
            (from.Kind == NodeKind.Setup && outputName == NodeSockets.Funds && input.Type == SocketType.Candidates)))
            return null;

        var expected = input?.Type.ToString() ?? inputName;

        return DraftProblem.OnNode("builder.problem.socket_mismatch", to, new { expected, actual = output?.ToString() ?? "" });
    }

    private static IEnumerable<DraftProblem> BindingProblems(StrategyGraph graph, DraftSetup setup)
    {
        foreach (var node in graph.Nodes.Where(node => node.Kind == NodeKind.Indicator))
        {
            var bindings = graph.Into(node.Id, NodeSockets.Of).ToList();
            if (bindings.Count > 1)
                yield return DraftProblem.OnNode("builder.problem.socket_mismatch", node, new { expected = "одне джерело ряду", actual = bindings.Count.ToString() });
            foreach (var link in bindings.Where(link => link.From == StrategyGraph.SetupId || link.From == PortfolioFlow.CashId))
            {
                var available = link.Output switch
                {
                    "funds" => setup.Funds.Count > 0, "benchmarkId" => setup.BenchmarkId is not null,
                    "cashId" or "series" when link.From == PortfolioFlow.CashId || link.From == StrategyGraph.SetupId => setup.CashId is not null,
                    "volatilityIndexId" => setup.VolatilityIndexId is not null,
                    _ when link.Output.StartsWith("extra:", StringComparison.Ordinal) && Guid.TryParse(link.Output[6..], out var id) => setup.ExtraSeries.Contains(id),
                    _ => false,
                };
                if (!available) yield return DraftProblem.OnNode("builder.problem.parameter_missing", node, new { field = link.Output });
            }
        }
    }

    private static IEnumerable<DraftProblem> CycleProblems(StrategyGraph graph)
    {
        var state = new Dictionary<NodeId, bool>();
        var inCycle = new HashSet<NodeId>();

        foreach (var node in graph.Nodes)
            Visit(node.Id, graph, state, inCycle);

        return graph.Nodes.Where(node => inCycle.Contains(node.Id)).Select(node => DraftProblem.OnNode("builder.problem.cycle", node));
    }

    private static IEnumerable<DraftProblem> PortfolioProblems(StrategyGraph graph)
    {
        foreach (var node in graph.Nodes)
            foreach (var input in NodeSockets.InputsOf(node.Kind).Where(input => !input.Many))
                if (graph.Into(node.Id, input.Name).Skip(1).Any())
                    yield return DraftProblem.OnNode("builder.problem.socket_mismatch", node, new { expected = "одне джерело", actual = "кілька з’єднань" });
        var path = PortfolioFlow.CandidatePath(graph);
        if (path.Count(node => node.Kind == NodeKind.FundSelection) > 1)
            yield return DraftProblem.OnNode("builder.problem.socket_mismatch", graph.ResultOf(BlockKind.Selection), new { expected = "один модуль відбору на потік", actual = "кілька модулів відбору" });
        foreach (var link in graph.Links)
        {
            var cashAsset = link.To == PortfolioFlow.CashId && link.Input == NodeSockets.Asset;
            var cashBudget = link.To == PortfolioFlow.CashId && link.Input == NodeSockets.Remainder;
            var portfolioBudget = link.To == StrategyGraph.SelectionResultId && link.Input == NodeSockets.Budget;
            var portfolioCash = link.To == PortfolioFlow.PortfolioId && link.Input == NodeSockets.Cash;
            var valid = portfolioCash ? link.From == PortfolioFlow.CashId && link.Output == "output"
                : cashAsset ? link.From == StrategyGraph.SetupId && link.Output == "cashId"
                : cashBudget ? link.From == StrategyGraph.SelectionResultId && link.Output == "cash"
                : portfolioBudget ? link.From == StrategyGraph.MarketResultId && link.Output == "output" : true;
            if (!valid && graph.Node(link.To) is { } target)
                yield return DraftProblem.OnNode("builder.problem.socket_mismatch", target, new { expected = portfolioCash ? "кеш після розподілу" : cashAsset ? "фонд для кешу" : cashBudget ? "залишок після розподілу" : "ринковий бюджет", actual = "інше джерело" });
        }
    }

    private static void Visit(NodeId id, StrategyGraph graph, Dictionary<NodeId, bool> onPath, HashSet<NodeId> inCycle)
    {
        if (onPath.TryGetValue(id, out var visiting))
        {
            if (visiting)
                inCycle.Add(id);
            return;
        }

        onPath[id] = true;
        foreach (var link in graph.OutOf(id).Where(link => !(id == PortfolioFlow.CashId && link.Output == "series")))
            Visit(link.To, graph, onPath, inCycle);
        onPath[id] = false;
    }

    private static IEnumerable<DraftProblem> MissingInputs(StrategyGraph graph)
    {
        foreach (var node in graph.Nodes)
        {
            var missing = NodeSockets.InputsOf(node.Kind).Where(input => input.Required && !graph.Into(node.Id, input.Name).Any());
            foreach (var input in missing)
                yield return DraftProblem.OnNode("builder.problem.input_missing", node, new { input = input.Name });

            if (node.Kind == NodeKind.Compare && !graph.Into(node.Id, NodeSockets.Right).Any() && !node.Parameters.Has(NodeSockets.Value))
                yield return DraftProblem.OnNode("builder.problem.input_missing", node, new { input = NodeSockets.Right });
        }
    }

    private static IEnumerable<DraftProblem> ParameterProblems(GraphNode node, StrategyGraph graph, RuleVocabulary vocabulary)
    {
        if (node.Kind == NodeKind.Setup) return [];
        if (node.Kind == NodeKind.Indicator)
            return IndicatorProblems(node, vocabulary);
        if (NodeKinds.IsCondition(node.Kind))
            return FieldProblems(node, vocabulary.Condition(NodeKinds.ConditionCode(node.Kind))?.Fields ?? []);

        return ResultProblems(node, graph, vocabulary);
    }

    private static IEnumerable<DraftProblem> IndicatorProblems(GraphNode node, RuleVocabulary vocabulary)
    {
        var kind = node.Parameters.Text("indicator") ?? "";
        if (vocabulary.Indicator(kind) is not { } indicator)
            return [DraftProblem.OnNode("builder.problem.indicator_unknown", node, new { kind })];

        return FieldProblems(node, indicator.Fields);
    }

    private static IEnumerable<DraftProblem> ResultProblems(GraphNode node, StrategyGraph graph, RuleVocabulary vocabulary)
    {
        var fields = ResultFields.Of(node.Kind).Select(name => vocabulary.BlockFields.GetValueOrDefault(name)).OfType<VocabularyField>().ToList();
        if (node.Kind != NodeKind.MarketResult)
            return FieldProblems(node, fields);

        var connected = graph.Into(node.Id, NodeSockets.Condition).Any();
        if (connected && !node.Parameters.Has(ResultFields.InvestWhenUnfavorable))
            return [DraftProblem.OnNode("builder.problem.market_filter_share_missing", node)];

        return connected ? FieldProblems(node, fields) : [];
    }

    private static IEnumerable<DraftProblem> FieldProblems(GraphNode node, IReadOnlyList<VocabularyField> fields)
    {
        foreach (var field in fields.Where(field => field.Kind != FieldKind.Indicator))
        {
            if (!node.Parameters.Has(field.Name))
            {
                if (field.Required)
                    yield return DraftProblem.OnNode("builder.problem.parameter_missing", node, new { field = field.Name });
                continue;
            }

            if (node.Parameters.Number(field.Name) is { } value && IsOutOfRange(field, value))
                yield return DraftProblem.OnNode("builder.problem.parameter_out_of_range", node, new { field = field.Name, min = field.Minimum, max = field.Maximum });
        }
    }

    private static bool IsOutOfRange(VocabularyField field, double value) =>
        field.Minimum is { } minimum && value < minimum || field.Maximum is { } maximum && value > maximum;

    private static IEnumerable<DraftProblem> SeriesProblems(GraphNode node, StrategyGraph graph, DraftSetup setup)
    {
        if (node.Kind != NodeKind.Indicator || SeriesChoice.Of(node.Parameters) is not { } series)
            yield break;

        if (graph.ResultsUsing(node.Id).Any(result => result.Kind == NodeKind.MarketResult) && series.Series == SeriesChoice.Fund)
            yield return DraftProblem.OnNode("builder.problem.fund_in_market_filter", node);
        if (series.Series == SeriesChoice.Extra && (series.InstrumentId is not { } instrument || !setup.ExtraSeries.Contains(instrument)))
            yield return DraftProblem.OnNode("builder.problem.extra_series_unknown", node);
    }

    private static IEnumerable<DraftProblem> ReportSignalProblems(StrategyDraft draft, IReadOnlyList<OwnSignal> signals)
    {
        foreach (var report in draft.Reports)
        {
            foreach (var id in report.SignalIds)
            {
                var signal = signals.FirstOrDefault(signal => signal.Id == id);
                if (signal is null || draft.MissingSignalIds.Contains(id))
                    yield return new DraftProblem("builder.problem.signal_deleted", new { id }, null, null, report.Code, ProblemSeverity.Error);
                else if (!signal.IsMarket)
                    yield return new DraftProblem("builder.problem.trigger_differs_in_backtest", new { name = signal.Name }, null, null, report.Code, ProblemSeverity.Warning);
            }
        }
    }

    private static IEnumerable<DraftProblem> SignalProblems(GraphNode node, StrategyGraph graph, IReadOnlyList<Guid> missing, IReadOnlyList<OwnSignal> signals)
    {
        if (node.Kind != NodeKind.Signal)
            yield break;

        var id = Guid.TryParse(node.Parameters.Text("signalId"), out var parsed) ? parsed : Guid.Empty;
        var signal = signals.FirstOrDefault(signal => signal.Id == id);
        if (signal is null || missing.Contains(id))
            yield return DraftProblem.OnNode("builder.problem.signal_deleted", node, new { id });
        else if (!signal.IsMarket)
            yield return DraftProblem.OnNode("builder.problem.signal_not_market", node);

        var isThisFundSignal = node.Kind == NodeKind.Signal && node.Parameters.Text("scope") == "thisFund";
        if (isThisFundSignal && graph.ResultsUsing(node.Id).Any(result => result.Kind == NodeKind.MarketResult))
            yield return DraftProblem.OnNode("builder.problem.this_fund_signal_in_market_filter", node);
    }

    private static IEnumerable<DraftProblem> DanglingNodes(StrategyGraph graph) =>
        graph.Nodes
            .Where(node => !NodeKinds.IsPinned(node.Kind) && !graph.OutOf(node.Id).Any())
            .Select(node => DraftProblem.OnNode("builder.problem.dangling_node", node, severity: ProblemSeverity.Warning));
}
