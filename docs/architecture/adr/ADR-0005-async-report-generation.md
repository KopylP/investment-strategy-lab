# ADR-0005: Reports are prepared in the background while the strategies think

- Status: accepted (2026-10-30)
- Context: [ADR-0003](ADR-0003-outbox-and-scheduler.md), [ADR-0004](ADR-0004-python-strategy-engine.md), D14

## Context
A report used to be generated synchronously: `POST /accounts/{id}/reports` and the scheduled job called the engine's `/decide` (`/risk`)
for each sub-portfolio in turn and waited for the answer within one HTTP request (the default resilience timeouts are tens of seconds).
An agent strategy (an LLM) may think for minutes or hours: it could not be plugged in that way, and one slow strategy delayed every report after it.

## Decision
- **The engine** has a background job API: `POST /v1/strategies/{code}/decide/jobs` (and `/risk/jobs`) checks the strategy and parameters at once
  and answers `202 { jobId }`; `GET /v1/jobs/{jobId}` gives `running`, `succeeded` with `result` (the same as `/decide` returns) or `failed` with `error`.
  Jobs live in memory (a thread pool, `STRATEGY_JOB_WORKERS`); after a restart the engine does not know them (`404`). The synchronous `/decide` and `/risk` remain.
- **Rebalancing** stores a `ReportRequest` (table `report_requests`): which report, for which sub-portfolios, who asked and when,
  and per sub-portfolio the engine job and its answer (jsonb). `Pending → Completed` (with `ReportId`) or `Failed` (with a reason).
- The button and the schedule only create a request (`202 { requestId }`, `Location: /api/rebalancing/report-requests/{id}`); while a request of the same code
  is waiting, asking again returns that one. `ReportRequestsJob` (every 5 s) submits jobs to the engine, polls them, resubmits lost ones
  (up to 5 times) and, when all have answered (or the deadline of 6 h has passed), builds the report with the same pipeline (`GenerateMonthlyReportHandler`,
  `GenerateRiskReviewHandler`) from what is held at that moment. The report and the completed request are saved in one transaction,
  and `ReportGeneratedV1` goes through the outbox — the notification about a ready report works as before.
- A strategy that did not answer or failed gives a warning in the report, as before; if none answered, the request is `Failed`.
- Notifications: a scheduled report — as before; an on-demand report — if the strategies thought for longer than a minute
  (`ReportGeneratedV1.RequestedAt`): the user has most likely left the page. A failed request publishes
  `ReportRequestFailedV1` — a notification with the reason by the same rule.
- The app shows requests (`GET /api/rebalancing/report-requests?accountId=`) as «Готується… (1 з 2 відповіли)» and opens the report
  as soon as it is ready; a failed request stays visible for a day.

## Consequences
- An agent plugs in as an ordinary engine strategy type (`decide`/`assess_risk`) that thinks as long as it needs; the backend does not depend on that.
- The schedule counts the time of the last scheduled *request*, so while the strategies think, the same report is not requested every minute.
- The strategies' answers refer to the state at the time of the request, and the plan to the state at the time it is built; positions may change in between (the plan is still computed from the actual ones).
- One instance of the application and of the engine: engine jobs do not survive its restart (they are resubmitted), and there is no distributed locking of requests.
