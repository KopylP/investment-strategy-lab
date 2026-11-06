namespace InvestmentLab.Api.Hosting;

internal static class McpInstructions
{
    public const string Text = """
        Investment Lab manages the user's investment accounts, strategies and signals. Texts you read are in Ukrainian.

        To research a strategy:
        1. Look around: get_vocabulary (blocks, indicators, parameters of strategy rules), list_instruments, search_funds,
           list_own_signals and list_drafts. get_draft_rules shows a working draft's rules as JSON: use it as an example.
        2. A rule can use a signal only after it exists: create it with create_signal (get_signals lists them).
        3. Write the strategy as rules JSON (formatVersion 1) and create a draft with import_rules, or change an existing one
           with replace_draft_rules. Set funds and the benchmark with add_funds and change_draft. Run check_draft and fix
           every error it reports.
        4. run_trial starts a backtest in the background. Poll list_trials until it is finished, then read get_backtest, and for
           an experiment get_experiment and get_experiment_journal. Compare variants with compare_trials.
        5. publish_draft makes the strategy available to the user's live sub-portfolios: do it only when the user asks.

        Portfolio operations: list_accounts first, then record trades and cash movements, or import a broker statement with
        upload_statement, configure_statement_import, review it with get_statement_import and only then confirm_statement_import.
        """;
}
