#!/usr/bin/env bash
# Prepares the add-on folder: published API (with the built Angular app in wwwroot) and the strategy engine source.
# Then copy deploy/home-assistant/investlab to the Home Assistant "addons" share (Samba or SSH) and install it as a
# local add-on; Home Assistant builds the image on the device.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/deploy/home-assistant/investlab/rootfs/app"

rm -rf "$OUT" && mkdir -p "$OUT/api" "$OUT/engine"

echo "==> Angular"
(cd "$ROOT/frontend" && npm ci --no-audit --no-fund && npx ng build --configuration production)

echo "==> .NET API (framework-dependent, runs on arm64 and amd64)"
dotnet publish "$ROOT/backend/src/Hosts/InvestmentLab.Api" -c Release -o "$OUT/api" -p:UseAppHost=false
mkdir -p "$OUT/api/wwwroot" && cp -R "$ROOT/frontend/dist/frontend/browser/." "$OUT/api/wwwroot/"

echo "==> Strategy engine"
cp "$ROOT/strategy-engine/pyproject.toml" "$OUT/engine/"
cp -R "$ROOT/strategy-engine/src" "$OUT/engine/src"
find "$OUT/engine" -name __pycache__ -type d -prune -exec rm -rf {} + ; rm -rf "$OUT/engine/src/"*.egg-info

echo "==> Home Assistant integration (MCP proxy)"
mkdir -p "$OUT/integration"
cp -R "$ROOT/deploy/home-assistant/integration/custom_components/investlab" "$OUT/integration/investlab"
find "$OUT/integration" -name __pycache__ -type d -prune -exec rm -rf {} +

du -sh "$OUT"
echo "Done: $ROOT/deploy/home-assistant/investlab"
