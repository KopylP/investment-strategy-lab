import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { NavigationError } from '@angular/router';
import { AuthStore } from './auth-store';

const reloadKey = 'investmentlab.ingressReloadAt';
const reloadCooldownMs = 30_000;

interface Hass {
  callWS<T>(message: Record<string, unknown>): Promise<T>;
}

@Injectable({ providedIn: 'root' })
export class IngressSession {
  private readonly document = inject(DOCUMENT);
  private readonly enabled = inject(AuthStore).inHomeAssistantPanel;
  private pending: Promise<boolean> | null = null;

  constructor() {
    if (this.enabled)
      this.document.addEventListener('visibilitychange', () => {
        if (this.document.visibilityState === 'visible') void this.renew();
      });
  }

  renew(): Promise<boolean> {
    if (!this.enabled) return Promise.resolve(false);
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
      return false;
    }
  }
}

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
