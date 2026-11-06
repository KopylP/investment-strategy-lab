"""Forwards MCP requests from Home Assistant's origin to the Investment Lab add-on.

The add-on authenticates every request itself: this view passes the caller's token on and adds nothing about the user.
"""

from aiohttp import ClientSession, ClientTimeout, web
from homeassistant.components.http import HomeAssistantView

from .const import MCP_PATH, RESOURCE_METADATA_PATH

FORWARDED_REQUEST_HEADERS = (
    "Accept",
    "Authorization",
    "Content-Type",
    "Last-Event-ID",
    "Mcp-Protocol-Version",
    "Mcp-Session-Id",
)
HOP_BY_HOP_HEADERS = {"connection", "content-length", "keep-alive", "transfer-encoding"}
NO_TOTAL_TIMEOUT = ClientTimeout(total=None, sock_connect=10, sock_read=600)


class AddonForwarder:
    """Sends one request to the add-on and streams the answer back."""

    def __init__(self, session: ClientSession, addon_url: str) -> None:
        self._session = session
        self._addon_url = addon_url.rstrip("/")

    async def forward(self, request: web.Request) -> web.StreamResponse:
        async with self._session.request(
            request.method,
            self._addon_url + request.path,
            headers=_request_headers(request),
            data=request.content if request.body_exists else None,
            allow_redirects=False,
            auto_decompress=False,
            timeout=NO_TOTAL_TIMEOUT,
        ) as upstream:
            response = web.StreamResponse(status=upstream.status, headers=_response_headers(upstream.headers))
            await response.prepare(request)
            async for chunk in upstream.content.iter_any():
                await response.write(chunk)
            await response.write_eof()
            return response


def _request_headers(request: web.Request) -> dict[str, str]:
    headers = {name: request.headers[name] for name in FORWARDED_REQUEST_HEADERS if name in request.headers}
    headers["X-Forwarded-Host"] = request.host
    headers["X-Forwarded-Proto"] = request.scheme
    return headers


def _response_headers(headers) -> dict[str, str]:
    return {name: value for name, value in headers.items() if name.lower() not in HOP_BY_HOP_HEADERS}


class McpView(HomeAssistantView):
    """The MCP endpoint, published on Home Assistant's own origin."""

    url = MCP_PATH
    name = "api:investlab:mcp"
    requires_auth = False
    cors_allowed = True

    def __init__(self, forwarder: AddonForwarder) -> None:
        self._forwarder = forwarder

    async def get(self, request: web.Request) -> web.StreamResponse:
        return await self._forwarder.forward(request)

    async def post(self, request: web.Request) -> web.StreamResponse:
        return await self._forwarder.forward(request)

    async def delete(self, request: web.Request) -> web.StreamResponse:
        return await self._forwarder.forward(request)


class ResourceMetadataView(HomeAssistantView):
    """RFC 9728 metadata of the MCP endpoint: where clients get a token (Home Assistant itself)."""

    url = RESOURCE_METADATA_PATH
    name = "api:investlab:mcp:resource-metadata"
    requires_auth = False
    cors_allowed = True

    def __init__(self, forwarder: AddonForwarder) -> None:
        self._forwarder = forwarder

    async def get(self, request: web.Request) -> web.StreamResponse:
        return await self._forwarder.forward(request)
