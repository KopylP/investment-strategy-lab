"""One confirmation step: the integration has nothing to configure, the add-on tells it where it lives."""

from homeassistant.config_entries import ConfigFlow, ConfigFlowResult

from .const import DOMAIN

ENTRY_TITLE = "Investment Lab MCP"


class InvestLabConfigFlow(ConfigFlow, domain=DOMAIN):
    VERSION = 1

    async def async_step_user(self, user_input: dict | None = None) -> ConfigFlowResult:
        if user_input is not None:
            return self.async_create_entry(title=ENTRY_TITLE, data={})
        return self.async_show_form(step_id="user")
