# Home Assistant add-on

One container (`investlab/`): PostgreSQL + the Python strategy engine + the .NET API serving the built Angular app,
reachable only through the Home Assistant Ingress panel. User docs (Ukrainian): `README.md` (install, deploy) and
`investlab/DOCS.md` (shown in Home Assistant — update it when notifications, events or options change).

- Installed from this repository as an add-on store (`repository.yaml` in the root) with a prebuilt aarch64 image
  (ADR-0028): `.github/workflows/addon-release.yml` runs `build.sh`, builds `ghcr.io/kopylp/investlab:<version>` and only then
  commits the version to `investlab/config.yaml` (`image:` there). Do not change `version` in ordinary commits.
- `build.sh` builds the artifacts (Angular build into the API's `wwwroot`, API publish, engine source) into
  `investlab/rootfs/app/` (generated, not committed). `deploy.sh` is for changes not on `main`: it builds, rsyncs to the
  Home Assistant host over SSH without the `image:` line and installs the local add-on (built on the device).
- `investlab/rootfs/run.sh` — the container entrypoint: initialises Postgres in `/data` once, exports or imports the data
  when asked by a marker in `/share/investlab`, reads add-on options from `/data/options.json`, starts the engine and the API.
- Users are identified by the Supervisor's `X-Remote-User-Id` header via Ingress; notifications go through the
  Home Assistant Core API with the Supervisor token.
- The API's memory is published every minute as `sensor.investlab_api_memory` and `sensor.investlab_api_managed_heap`
  (ADR-0032, `Hosting/HomeAssistantMemorySensors.cs`); option `otlp_endpoint` sets `OTEL_EXPORTER_OTLP_ENDPOINT`.
- MCP (option `mcp_enabled`, ADR-0023): `integration/custom_components/investlab/` is a Home Assistant integration that
  only forwards `/api/investlab/mcp` and its RFC 9728 metadata from Home Assistant's origin to 8098. `run.sh` copies it
  into Home Assistant's config with an `addon.json` naming this add-on's host. The API validates the caller's Home
  Assistant token by asking Home Assistant Core, so the forwarder adds nothing about the user.
