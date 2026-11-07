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
           with replace_draft_rules. Set funds and the benchmark with add_funds and change_draft (it changes only the setup
           fields you give). Run check_draft and fix every error it reports. The market filter's whenUnfavorable is
           reduceHoldings (an unfavourable market scales every target weight by investWhenUnfavorable and parks the rest in
           the cash fund) or newMoneyOnly (only new contributions are split; backtests have none, so it changes nothing there).
        4. Before choosing a period, get_instrument_history shows from when each fund, the benchmark and the cash fund have
           quotes; indicators need their warm-up on top of that. If a period still lacks history, the error names what limits
           it and the earliest possible start.
        5. run_trial starts a backtest in the background (rerun: true computes it again, e.g. after quotes were fixed;
           cancel_trial drops one still waiting). Poll list_trials until it is finished (queue position 0 = running).
        6. Read results without monthly points: get_experiment (summary, last 1/3/5 years, calendar years, robustness
           studies) or get_backtest; ask for points only when a chart needs them. Compare many trials with the benchmark
           over the same period in one trial_table call, two at a time with compare_trials.
        7. Diagnose behaviour with get_experiment_holdings (what was held, cash share, unfavourable rebalances, trades per
           year), then get_experiment_journal for single decisions; detail: true adds the ranked candidates.
        8. publish_draft makes the strategy available to the user's live sub-portfolios: do it only when the user asks.

        Portfolio operations: list_accounts first, then record trades and cash movements, or import a broker statement with
        upload_statement, configure_statement_import, review it with get_statement_import and only then confirm_statement_import.
        """;
}
