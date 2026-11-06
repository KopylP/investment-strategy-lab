# Home Assistant add-on

One container (`investlab/`): PostgreSQL + the Python strategy engine + the .NET API serving the built Angular app,
reachable only through the Home Assistant Ingress panel. User docs (Ukrainian): `README.md` (install, deploy) and
`investlab/DOCS.md` (shown in Home Assistant — update it when notifications, events or options change).

- `build.sh` builds the artifacts (Angular build into the API's `wwwroot`, API publish, engine source) into
  `investlab/rootfs/app/` (generated, not committed); `deploy.sh` builds,
  rsyncs to the Home Assistant host over SSH and installs/updates via the `ha` CLI.
- `investlab/config.yaml` — add-on manifest. Its `version` is bumped automatically on push to `main`
  (`.github/workflows/addon-version.yml`); do not change it in ordinary commits.
- `investlab/rootfs/run.sh` — the container entrypoint: initialises Postgres in `/data` once, reads add-on options
  from `/data/options.json`, starts the engine and the API.
- Users are identified by the Supervisor's `X-Remote-User-Id` header via Ingress; notifications go through the
  Home Assistant Core API with the Supervisor token.
- MCP (option `mcp_enabled`, ADR-0023): `integration/custom_components/investlab/` is a Home Assistant integration that
  only forwards `/api/investlab/mcp` and its RFC 9728 metadata from Home Assistant's origin to 8098. `run.sh` copies it
  into Home Assistant's config with an `addon.json` naming this add-on's host. The API validates the caller's Home
  Assistant token by asking Home Assistant Core, so the forwarder adds nothing about the user.
