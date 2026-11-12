# ADR-0030: Home Assistant backs up the add-on cold

- Status: accepted (2026-11-12)

## Context

The add-on keeps its PostgreSQL cluster in `/data/postgres`, and a Home Assistant backup of the add-on is a tar of `/data`.
`config.yaml` set no backup mode, so the Supervisor used the default, a hot backup: it copied the data directory while
PostgreSQL was running and writing to it. A file copy of a live cluster taken without `pg_backup_start` and the WAL written
during the copy is not guaranteed to start: data files and WAL can come from different moments. DOCS.md told the user that
the Home Assistant backup covers the database, so the one copy the user relies on could be one that does not restore.

Two ways make the copy consistent:

- **`backup: cold`.** The Supervisor stops the add-on, copies `/data`, and starts it again. `run.sh` stops PostgreSQL with
  `pg_ctl stop -m fast` on SIGTERM, so the copy is a shut-down cluster that starts like after any restart. Even when the stop
  times out and the container is killed, the files are at rest and PostgreSQL's crash recovery replays the WAL on start.
- **`backup_pre` / `backup_post` with a dump.** A pre-backup command writes `pg_dump` into `/data` and the restore path in
  `run.sh` loads it. The add-on keeps running, but the copy still holds the live data directory next to the dump, `run.sh`
  must decide on start which of the two is the truth, and an untested restore path is exactly what this decision is about.

## Decision

- **The add-on is backed up cold** (`backup: cold` in `config.yaml`). Nothing in `run.sh` changes: a restored `/data` is a
  stopped cluster, which the normal start already handles.
- The add-on is unavailable while the backup is written: the panel, MCP and notifications for a few minutes on a database
  of a few MB of quotes and a ledger. DOCS.md says so, and suggests scheduling automatic backups for a quiet hour.

## Consequences

- A Home Assistant backup of the add-on restores to a database that starts, without a separate restore step.
- Each backup restarts the add-on, so the jobs that run at startup (quotes, exchange rates, instrument descriptions) run
  again after it.
- If the stop ever has to keep the add-on running (for example, a long backup window), the `backup_pre` dump is the way to
  revisit, with a start in `run.sh` that restores from the dump and a test of that restore.
