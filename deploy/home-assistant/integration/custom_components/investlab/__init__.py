"""Investment Lab: publishes the add-on's MCP endpoint on Home Assistant's origin, behind Home Assistant's OAuth."""

import json
from pathlib import Path

from homeassistant.config_entries import ConfigEntry
from homeassistant.core import HomeAssistant
from homeassistant.exceptions import ConfigEntryNotReady
from homeassistant.helpers.aiohttp_client import async_get_clientsession

from .const import ADDON_FILE, DOMAIN, VIEWS_REGISTERED
from .proxy import AddonForwarder, McpView, ResourceMetadataView


async def async_setup_entry(hass: HomeAssistant, entry: ConfigEntry) -> bool:
    if hass.data.setdefault(DOMAIN, {}).get(VIEWS_REGISTERED):
        return True

    addon_url = await hass.async_add_executor_job(_read_addon_url)
    forwarder = AddonForwarder(async_get_clientsession(hass), addon_url)
    hass.http.register_view(McpView(forwarder))
    hass.http.register_view(ResourceMetadataView(forwarder))
    hass.data[DOMAIN][VIEWS_REGISTERED] = True
    return True


async def async_unload_entry(hass: HomeAssistant, entry: ConfigEntry) -> bool:
    return True


def _read_addon_url() -> str:
    addon_file = Path(__file__).parent / ADDON_FILE
    try:
        return json.loads(addon_file.read_text(encoding="utf-8"))["url"]
    except (OSError, ValueError, KeyError) as error:
        raise ConfigEntryNotReady(f"{addon_file} is missing: start the Investment Lab add-on with MCP enabled") from error
