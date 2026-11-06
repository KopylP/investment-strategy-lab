# ADR-0022: Experiments with fixed conditions and independent tests

- Status: accepted (2026-11-06)

## Context

An ordinary backtest belongs to a strategy version and is updated when the history changes. For comparing rules, the analyst needs a separate experiment: the same dates, capital, execution costs and a saved journal of decisions. The result of an independent period must not influence the choice of parameters.

## Decision

- Backtesting owns the experiments, their conditions and their results. StrategyBuilder stores its own description of the request and the experiment identifier in the trial run; it reaches Backtesting through a port and an adapter.
- The period, the costs, the plan of the independent check, walk-forward, the permitted neighbourhood of a parameter and the selection by the training Sharpe are rules of the Domain. The Application loads the data, calls these rules and stores the result; each action has its own handler.
- The initial capital and the commissions affect the actual quantities and payments of the simulation. The spread and the slippage change the execution price in an unfavourable direction; in the journal they are separated from the commission.
- Indicator warm-up may read older history, but the trades and the result belong only to the chosen period. The variants share the dates and the requirements for sufficient history.
- For an independent test and for each walk-forward step, a candidate is chosen only by the training window. With equal Sharpe, the original order of the candidates is kept; the first one is the current value of the parameter. A failed training gives a verification error, not an invented result.
- The check of neighbouring parameters over the whole period is shown separately from the independent tests. It is not evidence of an advantage on new data.
- The journal contains the decisions, the candidates and the indicator values, the target and actual weights, the executed and the expired orders. An execution refers to the date of its decision. The journal is read through a separate request with filters and pages.
- The experiment runs in the background. The scheduler only finds the next run and calls the handler in its own scope. The failure of one run does not block the others.

## Consequences

- Old trial runs without an experiment remain available.
- A plan with independent checks runs several passes and takes more time. The number of candidates and walk-forward steps is limited by the domain rules.
- The rules do not rewrite the saved version of the strategy: a numeric variant is passed to the evaluation as a verified parameter override only for the duration of the simulation.
- The new terms are entered in the [glossary](../../domain/glossary.md); the rules are checked by domain tests. An end-to-end check of the new scenario through the API and the database is still needed.
