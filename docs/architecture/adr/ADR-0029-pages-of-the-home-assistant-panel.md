# ADR-0029: The Home Assistant panel shows and opens pages of the app

- Status: accepted (2026-11-12)

## Context

In Home Assistant the app runs in the iframe of its sidebar panel at `/api/hassio_ingress/<token>/`. The panel always loads
the iframe at its root, and Home Assistant's address bar stayed at `/<slug>` whatever page was open: a reload went back to
the overview, an address could not be shared, and a notification could only open the panel, not the report it announced.

## Decision

- **The app talks to the panel through Home Assistant's own iframe messages.** It sends `home-assistant/subscribe-properties`
  and receives `home-assistant/properties` with the panel's route; after each navigation it sends `home-assistant/navigate`
  with `replace`, so the address bar shows `/<slug>/<page>` while Back and Forward keep working through the iframe's history
  (pushing in both would need two presses per step).
- **The page to start on is taken before the first navigation.** An app initializer waits up to a second for the first
  route and puts its page into the iframe's address, so `/<slug>/reports/<id>` opens the report without showing the overview.
- **The panel path is cut by Home Assistant's rule, not by the route's prefix.** Home Assistant 2026.2 splits the route one
  segment too far for a sidebar panel (prefix `/<slug>/reports`, path `/<id>`); later versions send it right. The app joins
  prefix and path and cuts the panel as Home Assistant itself does: `/<slug>`, or `/app/<slug>` for the generic app page.
- **A notification names a page of the app, a channel makes the link.** `Notification.Page` is the app's own path
  (`/reports/<id>`, `/signals`, `/profile`); the Home Assistant channel puts the panel path (`/<slug>` from `run.sh`) in front
  of it for the persistent notice and the phone push. Another channel would build its own address from the same page.
- **Home Assistant stays one plug-in.** Everything that knows the panel lives in `frontend/src/app/core/home-assistant/`
  and is added by `provideHomeAssistantPanel()`; outside the panel it does nothing and the app routes as any Angular app.
  `HomeAssistantIsolationTests` keeps the ingress path and the panel messages out of the rest of the frontend.

## Consequences

- A link or a reload opens the same page; a notification opens what it announces.
- A page's query string is not kept in the address bar: Home Assistant's route carries only the path.
- If Home Assistant drops the messages, the app keeps working inside the panel; only the address bar stops following it.
