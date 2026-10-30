import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { NavigationError } from '@angular/router';
import { AuthStore } from './auth-store';

/** When the panel last reloaded itself for a section's code (sessionStorage): not again this soon, so it cannot loop. */
const reloadKey = 'investmentlab.ingressReloadAt';
const reloadCooldownMs = 30_000;

/** What this needs of the Home Assistant frontend's `hass` object. */
interface Hass {
  callWS<T>(message: Record<string, unknown>): Promise<T>;
}

/**
 * Home Assistant lets the panel through with its own short Ingress session (the `ingress_session` cookie), which its
 * frontend renews only while it runs: after the phone slept every request gets the Supervisor's 401, and reloading
 * the panel's iframe only brings the Supervisor's bare "401: Unauthorized" page. The panel is served from Home
 * Assistant's origin, so it asks the Home Assistant frontend around it for a new session the same way that frontend
 * does (supervisor/api → /ingress/session, allowed for non-admins) and sets the same cookie.
 */
@Injectable({ providedIn: 'root' })
export class IngressSession {
  private readonly document = inject(DOCUMENT);
  private readonly enabled = inject(AuthStore).inHomeAssistantPanel;
  private pending: Promise<boolean> | null = null;

  constructor() {
    // Back from the background: a new session before the first request, not after its 401.
    if (this.enabled)
      this.document.addEventListener('visibilitychange', () => {
        if (this.document.visibilityState === 'visible') void this.renew();
      });
  }

  /** True once a new session cookie is set; false outside the panel or without the Home Assistant frontend around. */
  renew(): Promise<boolean> {
    if (!this.enabled) return Promise.resolve(false);
    // Several requests fail at once: one session for all of them.
    return (this.pending ??= this.open().finally(() => (this.pending = null)));
  }

  private async open(): Promise<boolean> {
    try {
      const hass = (this.document.defaultView?.parent?.document.querySelector('home-assistant') as { hass?: Hass } | null)?.hass;
      if (!hass?.callWS) return false;
      const { session } = await hass.callWS<{ session: string }>({ type: 'supervisor/api', endpoint: '/ingress/session', method: 'post' });
      const secure = this.document.location.protocol === 'https:' ? ';Secure' : '';
      this.document.cookie = `ingress_session=${session};path=/api/hassio_ingress/;SameSite=Strict${secure}`;
      return true;
    } catch {
      // Not inside Home Assistant's page (the Ingress URL opened on its own), or Home Assistant refused.
      return false;
    }
  }
}

/**
 * Router's navigation error handler: a section's code (a lazy chunk) that failed to load in the panel met the lapsed
 * session before any API request did. A browser may keep a failed module import, so after a new session the page
 * loads anew at that section rather than retrying the import.
 */
export function reloadWithNewIngressSession(error: NavigationError): void {
  const message = error.error instanceof Error ? error.error.message : String(error.error);
  if (!/dynamically imported module|Importing a module script failed/i.test(message)) return;
  const document = inject(DOCUMENT);
  void inject(IngressSession).renew().then((renewed) => {
    if (!renewed || recentlyReloaded(document.defaultView!)) return;
    document.location.assign(new URL(error.url.replace(/^\//, ''), document.baseURI).href);
  });
}

function recentlyReloaded(window: Window): boolean {
  try {
    const last = Number(window.sessionStorage.getItem(reloadKey));
    if (last && Date.now() - last < reloadCooldownMs) return true;
    window.sessionStorage.setItem(reloadKey, String(Date.now()));
    return false;
  } catch {
    return true;
  }
}
