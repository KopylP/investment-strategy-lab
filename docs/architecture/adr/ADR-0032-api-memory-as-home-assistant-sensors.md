# ADR-0032: The API publishes its memory to Home Assistant as sensors

- Status: accepted (2026-11-13)

## Context

Backtests and strategy experiments run inside the API process and keep years of quotes in memory (ADR-0014), so the
owner needs to see how much memory the process uses over time on the Home Assistant machine. The API already collects the
.NET runtime metrics through OpenTelemetry (`AddRuntimeInstrumentation`, which on .NET 9+ is the built-in `System.Runtime`
meter: `dotnet.process.memory.working_set`, `dotnet.gc.last_collection.heap.size` and others), but they leave the
process only through the OTLP exporter, which is on only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. The add-on never set
it, and a collector is one more service to install and run for one graph. The Supervisor shows the memory of the whole
add-on, which is PostgreSQL, the strategy engine and the API together. The only signal about the API alone was a log
warning when a replay starts above `Backtesting:Runs:MemoryWarningMegabytes`.

Home Assistant already keeps history and draws graphs of any sensor's state, and the add-on may call the Core API
(`homeassistant_api`), which it does for notifications.

## Decision

- **A second metrics exporter, beside OTLP, writes Home Assistant sensor states.** `HomeAssistantMemorySensors` in the
  API host (`Hosting/`) is an OpenTelemetry `BaseExporter<Metric>` behind a `PeriodicExportingMetricReader` (every minute).
  It takes the two memory metrics from the runtime metrics the API collects anyway and posts them to
  `core/api/states/<entity>` through the Supervisor: `sensor.investlab_api_memory` (working set) and
  `sensor.investlab_api_managed_heap` (managed heap after the last collection, summed over the generations), in MiB with
  `device_class: data_size` and `state_class: measurement`, so the recorder keeps their history and long-term statistics.
- **It belongs to the host, not a module.** The memory is the process's, and the host already owns what is particular to
  running in Home Assistant (Ingress, data directory, MCP forwarding). It is registered only when `SUPERVISOR_TOKEN` is
  set, so outside the add-on (Aspire, tests) nothing is added.
- **Names follow the catalogs.** The friendly names are message codes (`sensor.api_memory`, `sensor.api_managed_heap`)
  rendered in Home Assistant's own language, as notifications are when the user chose none.
- **An optional add-on option `otlp_endpoint`** passes through to `OTEL_EXPORTER_OTLP_ENDPOINT` for whoever runs a
  collector; it adds nothing the sensors need.

## Consequences

- The owner sees the API's memory in Home Assistant's history and on any dashboard, without installing anything.
- The sensors are states set through the REST API, not registry entities: they cannot be renamed in the UI and disappear
  after a Home Assistant restart until the next publish a minute later.
- Another runtime metric (CPU, GC pauses) is one more entry in the exporter's list. The strategy engine and PostgreSQL are
  separate processes and are not covered; the Supervisor's add-on figure minus the API sensor approximates them.
- A failed publish (Home Assistant restarting) is logged as a warning and retried on the next interval.
