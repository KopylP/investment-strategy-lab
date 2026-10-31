import { Location } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, NavigationStart, Router } from '@angular/router';

/** A page the user has been on in this visit; `id` is the router's id of the navigation stored with its history entry. */
interface VisitedPage {
  id: number;
  url: string;
  title: string;
}

/**
 * Follows the user's way through the app, in step with the browser's history, so «back» on a page leads to where they
 * really came from: the list of reports when «Змінити час» brought them to an account from there. A page opened
 * directly (a link in a notification, a reload) has no way behind it inside the app: it leads to its parent page.
 */
@Injectable({ providedIn: 'root' })
export class NavigationHistory {
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  private readonly pages = signal<VisitedPage[]>([]);
  private readonly position = signal(-1);

  /** The page «back» returns to; null when the visit began on this one. */
  readonly previous = computed(() => this.pages()[this.position() - 1] ?? null);

  constructor() {
    let restoredId: number | undefined;
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationStart) restoredId = event.restoredState?.navigationId;
      else if (event instanceof NavigationEnd) this.record(event, restoredId);
    });
  }

  /** Back through the browser's history, so its own back and forward stay in step; else to the parent page. */
  back(parentUrl: string): void {
    if (this.previous()) this.location.back();
    else void this.router.navigateByUrl(parentUrl);
  }

  private record(event: NavigationEnd, restoredId: number | undefined): void {
    const page: VisitedPage = { id: restoredId ?? event.id, url: event.urlAfterRedirects, title: this.titleOf(this.router.routerState.snapshot.root) };
    // Nobody goes back to the sign-in form.
    if (page.url.startsWith('/auth')) return this.restart([]);

    const pages = this.pages();
    const position = this.position();
    if (restoredId !== undefined) {
      // The browser's back or forward. An entry from before this visit (after a reload) has nothing known behind it.
      const known = pages.findIndex((visited) => visited.id === restoredId);
      if (known >= 0) this.position.set(known);
      else this.restart([page]);
    } else if (this.router.currentNavigation()?.extras.replaceUrl && position >= 0) {
      this.restart([...pages.slice(0, position), page]);
    } else {
      this.restart([...pages.slice(0, position + 1), page]);
    }
  }

  private restart(pages: VisitedPage[]): void {
    this.pages.set(pages);
    this.position.set(pages.length - 1);
  }

  private titleOf(route: ActivatedRouteSnapshot): string {
    return route.firstChild ? this.titleOf(route.firstChild) : (route.title ?? '');
  }
}
