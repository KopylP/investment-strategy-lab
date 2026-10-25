#!/usr/bin/env bash
# Investment Lab add-on: PostgreSQL (data in /data), the Python strategy engine, the .NET API that also serves the app.
set -euo pipefail

OPTIONS=/data/options.json
option() { jq -r --arg key "$1" '.[$key] // empty' "$OPTIONS" 2>/dev/null || true; }

export TZ="$(option timezone)"; export TZ="${TZ:-Europe/Warsaw}"
PGBIN="$(ls -d /usr/lib/postgresql/*/bin | sort -V | tail -1)"
PGDATA=/data/postgres
mkdir -p /data/keys

# --- PostgreSQL: initialised once, password generated once and kept in /data -----------------------------------
if [ ! -s "$PGDATA/PG_VERSION" ]; then
  echo "[investlab] initialising PostgreSQL in $PGDATA"
  mkdir -p "$PGDATA" && chown postgres:postgres "$PGDATA" && chmod 700 "$PGDATA"
  openssl rand -hex 24 > /data/db-password && chmod 600 /data/db-password
  install -o postgres -m 600 /data/db-password /tmp/pwfile
  su postgres -c "$PGBIN/initdb -D $PGDATA --username=postgres --pwfile=/tmp/pwfile --auth=scram-sha-256 --encoding=UTF8 --locale=C.UTF-8"
  rm -f /tmp/pwfile
fi
DB_PASSWORD="$(cat /data/db-password)"
mkdir -p /var/run/postgresql && chown postgres:postgres /var/run/postgresql
# Small footprint for a Raspberry Pi: the database is a few MB of quotes and a ledger.
su postgres -c "$PGBIN/pg_ctl -D $PGDATA -w -l /data/postgres.log -o \"-c listen_addresses=127.0.0.1 -c shared_buffers=48MB -c max_connections=30 -c work_mem=4MB\" start"
export PGPASSWORD="$DB_PASSWORD"
"$PGBIN/psql" -h 127.0.0.1 -U postgres -tc "SELECT 1 FROM pg_database WHERE datname='investmentlab'" | grep -q 1 \
  || "$PGBIN/psql" -h 127.0.0.1 -U postgres -c "CREATE DATABASE investmentlab"

# --- Strategy engine (FastAPI), only reachable from inside the add-on ------------------------------------------
/opt/engine/bin/uvicorn finance_decisions.api.main:app --host 127.0.0.1 --port 8000 --workers 1 &
ENGINE_PID=$!

# --- API + app ------------------------------------------------------------------------------------------------
export ConnectionStrings__investmentlab="Host=127.0.0.1;Port=5432;Database=investmentlab;Username=postgres;Password=$DB_PASSWORD"
export Strategies__StrategyEngineUrl="http://127.0.0.1:8000"
export MarketData__StrategyEngineUrl="http://127.0.0.1:8000"
export InvestmentLab__PathBase="/investlab"
export InvestmentLab__DataDirectory="/data"
# Ingress (Home Assistant panel) always talks plain http to 8098; 8099 is the direct port.
export Kestrel__Endpoints__Ingress__Url="http://0.0.0.0:8098"

if [ "$(option ssl)" = "true" ]; then
  CERT="/ssl/$(option certfile)"; KEY="/ssl/$(option keyfile)"
  if [ ! -f "$CERT" ] || [ ! -f "$KEY" ]; then
    # No certificate in /ssl: a self-signed one for the local network, made once and kept.
    CERT=/data/tls/cert.pem; KEY=/data/tls/key.pem
    if [ ! -f "$CERT" ]; then
      echo "[investlab] /ssl has no certificate: creating a self-signed one for homeassistant.local"
      mkdir -p /data/tls
      openssl req -x509 -newkey rsa:2048 -nodes -days 3650 -subj "/CN=homeassistant.local" \
        -addext "subjectAltName=DNS:homeassistant.local,DNS:homeassistant,DNS:localhost" -keyout "$KEY" -out "$CERT" 2>/dev/null
    fi
  fi
  export Kestrel__Endpoints__Direct__Url="https://0.0.0.0:8099"
  export Kestrel__Endpoints__Direct__Certificate__Path="$CERT"
  export Kestrel__Endpoints__Direct__Certificate__KeyPath="$KEY"
else
  export Kestrel__Endpoints__Direct__Url="http://0.0.0.0:8099"
fi

cd /app/api
dotnet InvestmentLab.Api.dll &
API_PID=$!

shutdown() {
  echo "[investlab] stopping"
  kill "$API_PID" "$ENGINE_PID" 2>/dev/null || true
  wait "$API_PID" "$ENGINE_PID" 2>/dev/null || true
  su postgres -c "$PGBIN/pg_ctl -D $PGDATA -m fast stop" || true
}
trap shutdown TERM INT

# If any part dies, stop the rest: the Supervisor restarts the add-on (watchdog) or shows it as stopped.
wait -n "$API_PID" "$ENGINE_PID" || true
shutdown
exit 1
