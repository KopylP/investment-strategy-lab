#!/usr/bin/env bash
# One-command deploy to Home Assistant: build the artifacts (build.sh), copy the add-on folder over SSH (the Terminal &
# SSH add-on) and let the Supervisor install it, or update/rebuild and restart it. The version comes from main: every
# merge there bumps it (.github/workflows/addon-version.yml), so pull first.
#
#   deploy/home-assistant/deploy.sh                    # build, copy, install or update to the version in config.yaml
#   deploy/home-assistant/deploy.sh --skip-build       # reuse the artifacts already in investlab/rootfs/app
#   deploy/home-assistant/deploy.sh --bump             # bump the patch version locally (a deploy off main)
#   deploy/home-assistant/deploy.sh --version 1.1.0    # set an exact version
#   HA_HOST=192.168.1.10 deploy/home-assistant/deploy.sh
#
# Needs: ssh and rsync locally; the Terminal & SSH add-on with your key in authorized_keys (it has rsync and `ha`).
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
ADDON_DIR="$HERE/investlab"
CONFIG="$ADDON_DIR/config.yaml"
SLUG="local_investlab"

HA_HOST="${HA_HOST:-homeassistant.local}"
HA_USER="${HA_USER:-root}"
HA_PORT="${HA_PORT:-22}"
BUILD=1
BUMP=0
NEW_VERSION=""
START=1
LOGS=1

usage() { sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'; cat <<EOF

Options:
  --host HOST        Home Assistant host (HA_HOST, default homeassistant.local)
  --user USER        SSH user (HA_USER, default root)
  --port PORT        SSH port (HA_PORT, default 22)
  --skip-build       do not run build.sh
  --bump             bump the patch version in config.yaml (the default keeps it)
  --version X.Y.Z    set this version instead of bumping the patch number
  --no-start         do not start the add-on after install/update
  --no-logs          do not show the add-on log at the end
  -h, --help         this help
EOF
}

while [ $# -gt 0 ]; do
  case "$1" in
    --host) HA_HOST="$2"; shift 2 ;;
    --user) HA_USER="$2"; shift 2 ;;
    --port) HA_PORT="$2"; shift 2 ;;
    --skip-build) BUILD=0; shift ;;
    --bump) BUMP=1; shift ;;
    --no-bump) BUMP=0; shift ;;
    --version) NEW_VERSION="$2"; shift 2 ;;
    --no-start) START=0; shift ;;
    --no-logs) LOGS=0; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
done

for tool in ssh rsync; do
  command -v "$tool" >/dev/null || { echo "Missing $tool" >&2; exit 1; }
done

# One SSH connection for every step: a password or key passphrase is asked once.
CONTROL_PATH="${TMPDIR:-/tmp}/investlab-deploy-$$"
SSH_OPTS="-p $HA_PORT -o ControlMaster=auto -o ControlPath=$CONTROL_PATH -o ControlPersist=120 -o ConnectTimeout=10"
cleanup() { ssh $SSH_OPTS -O exit "$HA_USER@$HA_HOST" 2>/dev/null || true; }
trap cleanup EXIT
remote() { ssh $SSH_OPTS "$HA_USER@$HA_HOST" "$@"; }

echo "==> Connecting to $HA_USER@$HA_HOST:$HA_PORT"
remote 'command -v ha >/dev/null && command -v rsync >/dev/null' \
  || { echo "The host needs the Terminal & SSH add-on (ha CLI and rsync)" >&2; exit 1; }

# Newer Home Assistant calls add-ons "apps" (and the shared folder /local_apps); older ones use "addons" and /addons.
HA_CMD="$(remote 'ha apps --help >/dev/null 2>&1 && echo apps || echo addons')"
REMOTE_DIR="$(remote '[ -d /local_apps ] && echo /local_apps || echo /addons')"

current_version() { sed -n 's/^version: *"\{0,1\}\([^"]*\)"\{0,1\} *$/\1/p' "$CONFIG"; }
VERSION="$(current_version)"
OLD_VERSION="$VERSION"
if [ -n "$NEW_VERSION" ] || [ "$BUMP" -eq 1 ]; then
  if [ -z "$NEW_VERSION" ]; then
    NEW_VERSION="$(echo "$VERSION" | awk -F. -v OFS=. '{ $NF = $NF + 1; print }')"
  fi
  # Portable in-place edit (BSD and GNU sed differ on -i).
  sed "s/^version: .*/version: \"$NEW_VERSION\"/" "$CONFIG" > "$CONFIG.tmp" && mv "$CONFIG.tmp" "$CONFIG"
  echo "==> Version $VERSION -> $NEW_VERSION"
  VERSION="$NEW_VERSION"
fi

if [ "$BUILD" -eq 1 ]; then
  "$HERE/build.sh"
elif [ ! -d "$ADDON_DIR/rootfs/app/api" ]; then
  echo "No artifacts in $ADDON_DIR/rootfs/app: run without --skip-build" >&2; exit 1
fi

echo "==> Copying the add-on to $HA_HOST:$REMOTE_DIR/investlab"
rsync -az --delete --exclude .gitignore -e "ssh $SSH_OPTS" "$ADDON_DIR" "$HA_USER@$HA_HOST:$REMOTE_DIR/"

echo "==> Reloading the add-on store"
remote "ha store reload --no-progress >/dev/null 2>&1 || ha $HA_CMD reload --no-progress"

# `ha <cmd> info` prints YAML; top-level keys are not indented. version is null until the add-on is installed.
info() { remote "ha $HA_CMD info $SLUG --no-progress" | sed -n "s/^$1: *//p" | head -n 1; }
INSTALLED="$(info version)"

if [ -z "$INSTALLED" ] || [ "$INSTALLED" = "null" ]; then
  echo "==> Installing $SLUG $VERSION (the image is built on the device, ~5-10 min on a Raspberry Pi)"
  remote "ha $HA_CMD install $SLUG"
elif [ "$INSTALLED" != "$VERSION" ]; then
  echo "==> Updating $SLUG $INSTALLED -> $VERSION (image build on the device, ~5-10 min on a Raspberry Pi)"
  remote "ha $HA_CMD update $SLUG"
else
  echo "==> Rebuilding $SLUG $VERSION (same version)"
  remote "ha $HA_CMD rebuild $SLUG"
fi

if [ "$START" -eq 1 ] && [ "$(info state)" != "started" ]; then
  echo "==> Starting $SLUG"
  remote "ha $HA_CMD start $SLUG"
fi

echo "==> $SLUG: version $(info version), state $(info state)"
if [ "$LOGS" -eq 1 ]; then
  sleep 5
  remote "ha $HA_CMD logs $SLUG" | tail -n 30 || true
fi

echo "Done: http://$HA_HOST:8099/investlab"
if [ "$VERSION" != "$OLD_VERSION" ]; then echo "config.yaml now says $VERSION: commit it."; fi
