# ADR-0023: MCP through Home Assistant

- Status: accepted (2026-11-06)

## Context

An AI assistant must work with Investment Lab without a UI: build strategies and the signals they use, run and read backtests, record operations. The service lives as a Home Assistant add-on and is reachable only through Ingress, which admits only the browser session of Home Assistant. Cloud MCP clients (claude.ai, chatgpt.com) know only MCP OAuth and must reach the service from the internet, while Home Assistant Cloud exposes to the outside only Home Assistant itself.

## Decision

- **One MCP endpoint in the same API** (`/api/investlab/mcp`, the official `ModelContextProtocol.AspNetCore`, stateless mode). The tools lie in the `Api` layer of each module next to the endpoints and call the same handlers, so the rules, the owner filter and the errors are the same as in the UI. The host collects the tools from the module assemblies (`WithToolsFromAssembly`).
- **Home Assistant is the authorization server; the service only verifies tokens.** The RFC 9728 metadata and the 401 challenge come from the MCP authentication of the SDK. The token is accepted by `JwtBearer` with its own `TokenHandler`: it asks Home Assistant Core (`auth/current_user` over a WebSocket) whose token it is, and the answer is cached briefly (`HybridCache`, for no more than 5 minutes and no longer than the token's lifetime). A Home Assistant user finds our user through the same external login as in Ingress. An unbound user gets a 403 with an explanation, not a new user.
- **The small Home Assistant integration only forwards** `/api/investlab/mcp` and its metadata from the Home Assistant origin into the add-on. It adds nothing about the user. The add-on installs it into the Home Assistant configuration itself when `mcp_enabled` is on. It takes the public address of the API from the standard `X-Forwarded-Host`/`-Proto`, which it trusts only from the address of Home Assistant Core.
- **The "Ingress only" guard lets through only the MCP paths**, and only when MCP is enabled.
- Enabling is an add-on option. An enabled MCP without an issuer of tokens does not start.

## Consequences

- Home Assistant tokens are not bound to our resource (no audience): a token issued to a client also opens the rest of Home Assistant. For a personal deployment this is acceptable; the built-in MCP server of Home Assistant works the same way.
- Anyone on the add-on network can reach `/api/investlab/mcp`, but without a valid Home Assistant token it gets nothing. The add-on has write access to the Home Assistant configuration, in order to install the integration.
- Without Home Assistant, the service is deployed with a different token verifier: its own authorization server (`Authority` and `Audience` in `JwtBearer` instead of our `TokenHandler`). The tools, the schema and the user binding do not change.
- The texts for the assistant are in English (the server instructions, the tool descriptions); messages and errors in the results are translated through the catalog, because the MCP response does not pass through the frontend translator.
