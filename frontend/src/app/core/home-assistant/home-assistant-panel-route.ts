import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { isInHomeAssistantPanel } from './home-assistant-panel';

interface PanelRoute {
  prefix: string;
  path: string;
}

interface PanelMessage {
  type?: string;
  route?: PanelRoute;
}

const subscribeProperties = 'home-assistant/subscribe-properties';
const properties = 'home-assistant/properties';
const navigate = 'home-assistant/navigate';
const firstRouteTimeoutMs = 1000;
const genericAppsPanel = 'app';

@Injectable({ providedIn: 'root' })
export class HomeAssistantPanelRoute {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly window = this.document.defaultView!;
  private readonly enabled =
    isInHomeAssistantPanel(this.document) && this.window.parent !== this.window;

  private panelPath: string | null = null;
  private shownPath: string | null = null;
  private readonly postedPaths: string[] = [];

  async restore(): Promise<void> {
    if (!this.enabled) return;

    const firstRoute = this.nextRoute();
    this.window.addEventListener('message', (event) => this.receive(event));
    this.post({ type: subscribeProperties });

    const route = await firstRoute;
    if (route) this.startAt(route);

    this.mirrorNavigations();
  }

  private nextRoute(): Promise<PanelRoute | null> {
    return new Promise((resolve) => {
      const timeout = setTimeout(() => resolve(null), firstRouteTimeoutMs);
      const listener = (event: MessageEvent) => {
        const route = this.routeIn(event);
        if (!route) return;

        clearTimeout(timeout);
        this.window.removeEventListener('message', listener);
        resolve(route);
      };

      this.window.addEventListener('message', listener);
    });
  }

  private startAt(route: PanelRoute): void {
    const outerPath = outerPathOf(route);
    this.panelPath = panelPathOf(outerPath);
    this.shownPath = outerPath;

    const page = this.pageOf(route);
    if (page === '/') return;

    const url = new URL(page.slice(1), this.document.baseURI);
    this.window.history.replaceState(this.window.history.state, '', url.href);
  }

  private receive(event: MessageEvent): void {
    const route = this.routeIn(event);
    if (!route || this.panelPath === null) return;

    const outerPath = outerPathOf(route);
    if (this.isEcho(outerPath)) return;

    this.shownPath = outerPath;

    const page = this.pageOf(route);
    if (page !== pathOf(this.router.url)) void this.router.navigateByUrl(page);
  }

  private mirrorNavigations(): void {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.show(pathOf(event.urlAfterRedirects)));
  }

  private show(page: string): void {
    if (this.panelPath === null) return;

    const outerPath = page === '/' ? this.panelPath : `${this.panelPath}${page}`;
    if (outerPath === this.shownPath) return;

    this.shownPath = outerPath;
    this.postedPaths.push(outerPath);
    this.post({ type: navigate, path: outerPath, options: { replace: true } });
  }

  private isEcho(outerPath: string): boolean {
    const posted = this.postedPaths.indexOf(outerPath);
    if (posted < 0) return false;

    this.postedPaths.splice(0, posted + 1);

    return true;
  }

  private pageOf(route: PanelRoute): string {
    return outerPathOf(route).slice(this.panelPath!.length) || '/';
  }

  private routeIn(event: MessageEvent): PanelRoute | null {
    if (event.source !== this.window.parent || event.origin !== this.window.location.origin)
      return null;

    const message = event.data as PanelMessage | null;

    return message?.type === properties && message.route ? message.route : null;
  }

  private post(message: Record<string, unknown>): void {
    this.window.parent.postMessage(message, this.window.location.origin);
  }
}

function outerPathOf(route: PanelRoute): string {
  return `${route.prefix}${route.path}`;
}

function panelPathOf(outerPath: string): string {
  const segments = outerPath.split('/').filter(Boolean);
  const panelSegments = segments[0] === genericAppsPanel ? 2 : 1;

  return `/${segments.slice(0, panelSegments).join('/')}`;
}

function pathOf(url: string): string {
  return url.split(/[?#]/)[0] || '/';
}
